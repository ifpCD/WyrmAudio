using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

// Exact SH projection of spherical triangles, after Wang & Ramamoorthi 2018, "Analytic Spherical Harmonic Coefficients
// for Polygonal Area Lights". Per triangle: axial moments tau_k(w) = integral of (w.u)^k, k <= order, over a fixed
// direction set W, from Arvo's boundary recurrences. Per mesh: SH = MomentToHarmonic * sum(tau), which folds Legendre
// monomials and a least-squares zonal harmonic factorization over W. Double precision: in float the recurrences
// cancel catastrophically for small triangles.
internal static unsafe class PolygonProjection
{
    public const int ORDER = HC.MAX_AMBISONIC_ORDER;

    // >= 2 * order + 3 moment directions, in lanes of 4, keeps every band's zonal factorization well conditioned
    public const int GROUPS = (2 * ORDER + 6) / 4;
    public const int MOMENTS = (ORDER + 1) * GROUPS;
    public const int BANDED_MOMENTS = HC.MAX_AMBISONIC_BANDS * MOMENTS;

    public const int SCRATCH = MOMENTS + ORDER;

    // (order * angular radius)^2 below which the centroid rule (error ~1e-5) is more accurate than the recurrences
    const double POINT_RULE_SPREAD = 1e-5;

    struct Arc
    {
        public double3 Start;
        public double3 End;
        public double3 Normal;
        public double3 Tangent;
        public double Sine;
        public double Cosine;
        public double Angle;

        public Arc(double3 start, double3 end)
        {
            double3 axis = math.cross(start, end);
            double sine = math.length(axis);
            double cosine = math.dot(start, end);
            double inverseSine = sine > 1e-15 ? 1.0 / sine : 0.0;

            Start = start;
            End = end;
            Sine = sine;
            Cosine = cosine;
            Angle = math.atan2(sine, cosine);
            Normal = axis * inverseSine;
            Tangent = (end - cosine * start) * inverseSine;
        }
    }

    // bandMoments: one BandedMoments block; within a band, [order][direction group]
    public static void AccumulateTriangle(
        double3 p0,
        double3 p1,
        double3 p2,
        double3 bandWeights,
        double4* directionsX,
        double4* directionsY,
        double4* directionsZ,
        double4* scratch,
        double4* bandMoments
    )
    {
        double3 lengths = new(math.length(p0), math.length(p1), math.length(p2));

        if (math.cmin(lengths) < 1e-6)
            return;

        double3 v0 = p0 / lengths.x;
        double3 v1 = p1 / lengths.y;
        double3 v2 = p2 / lengths.z;
        double triple = math.dot(v0, math.cross(v1, v2));

        if (triple < 0.0)
        {
            (v2, v1) = (v1, v2);
            triple = -triple;
        }

        // listener in the triangle's plane
        if (triple < 1e-14)
            return;

        double solidAngle = 2.0 * math.atan2(triple, 1.0 + math.dot(v0, v1) + math.dot(v1, v2) + math.dot(v2, v0));
        double3 centroid = math.normalize(v0 + v1 + v2);
        double spread = math.cmax(new double3(math.distancesq(v0, centroid), math.distancesq(v1, centroid), math.distancesq(v2, centroid)));

        double4* moments = scratch;

        if (spread * (ORDER * ORDER) < POINT_RULE_SPREAD)
            PointMoments(centroid, solidAngle, directionsX, directionsY, directionsZ, moments);
        else
            ArcMoments(v0, v1, v2, solidAngle, directionsX, directionsY, directionsZ, scratch + MOMENTS, moments);

        for (int band = 0; band < HC.MAX_AMBISONIC_BANDS; band++)
        {
            double weight = bandWeights[band];
            double4* target = BandedMoments.GetBand(bandMoments, band);

            for (int index = 0; index < MOMENTS; index++)
                target[index] += weight * moments[index];
        }
    }

    public static void Resolve(double4* bandMoments, double4* momentToHarmonic, int channels, double* harmonics)
    {
        for (int channel = 0; channel < channels; channel++)
        {
            double4* row = momentToHarmonic + channel * MOMENTS;
            double4 sum = 0.0;

            for (int index = 0; index < MOMENTS; index++)
                sum += row[index] * bandMoments[index];

            harmonics[channel] = math.csum(sum);
        }
    }

    static void PointMoments(double3 direction, double solidAngle, double4* directionsX, double4* directionsY, double4* directionsZ, double4* moments)
    {
        for (int group = 0; group < GROUPS; group++)
        {
            double4 alignment = directionsX[group] * direction.x + directionsY[group] * direction.y + directionsZ[group] * direction.z;
            double4 power = solidAngle;

            for (int order = 0; order <= ORDER; order++)
            {
                moments[order * GROUPS + group] = power;
                power *= alignment;
            }
        }
    }

    static void ArcMoments(
        double3 v0,
        double3 v1,
        double3 v2,
        double solidAngle,
        double4* directionsX,
        double4* directionsY,
        double4* directionsZ,
        double4* boundary,
        double4* moments
    )
    {
        Arc arc0 = new(v0, v1);
        Arc arc1 = new(v1, v2);
        Arc arc2 = new(v2, v0);

        for (int group = 0; group < GROUPS; group++)
        {
            AccumulateArcs(arc0, arc1, arc2, directionsX[group], directionsY[group], directionsZ[group], boundary);

            // (n + 1) tau_n = (n - 1) tau_(n-2) + sum over arcs of (w.normal) E_(n-1)
            double4 previous = 0.0;
            double4 current = solidAngle;
            moments[group] = current;

            for (int order = 1; order <= ORDER; order++)
            {
                double4 next = ((order - 1) * previous + boundary[order - 1]) * (1.0 / (order + 1));
                moments[order * GROUPS + group] = next;
                previous = current;
                current = next;
            }
        }
    }

    // E_k = integral over an arc of (w.u)^k; with f = w.u and f' = g along the arc: k E_k = (k - 1) |w_plane|^2 E_(k-2) - [f^(k-1) g].
    // The three arcs advance in lockstep so their independent recurrences overlap instead of serializing on latency.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AccumulateArcs(in Arc arc0, in Arc arc1, in Arc arc2, double4 x, double4 y, double4 z, double4* boundary)
    {
        var state0 = new ArcRecurrence(arc0, x, y, z);
        var state1 = new ArcRecurrence(arc1, x, y, z);
        var state2 = new ArcRecurrence(arc2, x, y, z);

        boundary[0] = state0.Normal * state0.Current + state1.Normal * state1.Current + state2.Normal * state2.Current;

        for (int order = 1; order < ORDER; order++)
        {
            double reciprocal = 1.0 / order;
            boundary[order] = state0.Advance(order, reciprocal) + state1.Advance(order, reciprocal) + state2.Advance(order, reciprocal);
        }
    }

    struct ArcRecurrence
    {
        public double4 Normal;
        public double4 Current;

        double4 _start;
        double4 _end;
        double4 _tangent;
        double4 _planeSquared;
        double4 _endSlope;
        double4 _previous;
        double4 _startPower;
        double4 _endPower;

        public ArcRecurrence(in Arc arc, double4 x, double4 y, double4 z)
        {
            _start = x * arc.Start.x + y * arc.Start.y + z * arc.Start.z;
            _end = x * arc.End.x + y * arc.End.y + z * arc.End.z;
            _tangent = x * arc.Tangent.x + y * arc.Tangent.y + z * arc.Tangent.z;
            Normal = x * arc.Normal.x + y * arc.Normal.y + z * arc.Normal.z;

            _planeSquared = _start * _start + _tangent * _tangent;
            _endSlope = _tangent * arc.Cosine - _start * arc.Sine;

            _previous = 0.0;
            Current = arc.Angle;
            _startPower = 1.0;
            _endPower = 1.0;
        }

        // (w.normal) E_order
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 Advance(int order, double reciprocal)
        {
            double4 next = ((order - 1) * _planeSquared * _previous - (_endPower * _endSlope - _startPower * _tangent)) * reciprocal;

            _previous = Current;
            Current = next;
            _startPower *= _start;
            _endPower *= _end;

            return Normal * next;
        }
    }

    // Main thread, once per registry lifetime.
    public static void BuildTables(
        NativeArray<double4> directionsX,
        NativeArray<double4> directionsY,
        NativeArray<double4> directionsZ,
        NativeArray<double4> momentToHarmonic
    )
    {
        const int directionCount = GROUPS * 4;
        const int channels = HC.MAX_AMBISONIC_CHANNELS;

        var directions = new double3[directionCount];
        var basis = new double[directionCount, channels];
        double* evaluated = stackalloc double[channels];

        for (int i = 0; i < directionCount; i++)
        {
            // spherical Fibonacci lattice; the 0.1 offset keeps every band's factorization conditioned below 13 up to order 10
            double t = i + 0.1;
            double z = 1.0 - 2.0 * t / directionCount;
            double radius = math.sqrt(1.0 - z * z);
            double azimuth = t * math.PI_DBL * (3.0 - math.sqrt(5.0));

            directions[i] = new double3(radius * math.cos(azimuth), radius * math.sin(azimuth), z);
            SphericalHarmonics.EvaluateAmbisonic(directions[i], evaluated);

            for (int channel = 0; channel < channels; channel++)
                basis[i, channel] = evaluated[channel];
        }

        for (int group = 0; group < GROUPS; group++)
        {
            int i = group * 4;
            directionsX[group] = new double4(directions[i].x, directions[i + 1].x, directions[i + 2].x, directions[i + 3].x);
            directionsY[group] = new double4(directions[i].y, directions[i + 1].y, directions[i + 2].y, directions[i + 3].y);
            directionsZ[group] = new double4(directions[i].z, directions[i + 1].z, directions[i + 2].z, directions[i + 3].z);
        }

        double[,] legendre = LegendreMonomials();
        var harmonicFromMoment = new double[channels, ORDER + 1, directionCount];

        for (int l = 0; l <= ORDER; l++)
        {
            int width = 2 * l + 1;
            int first = l * l;
            var normal = new double[width, width];

            for (int row = 0; row < width; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    double sum = 0.0;

                    for (int i = 0; i < directionCount; i++)
                        sum += basis[i, first + row] * basis[i, first + column];

                    normal[row, column] = sum;
                }
            }

            Invert(normal, width);

            // integral of P_l(w_i.u) = 4pi / (2l + 1) * sum_m Y_lm(w_i) c_lm  ->  c_l = (2l + 1) / 4pi * pinv(B_l) * L_l
            double scale = (2.0 * l + 1.0) / (4.0 * math.PI_DBL);

            for (int m = 0; m < width; m++)
            {
                for (int i = 0; i < directionCount; i++)
                {
                    double reconstruction = 0.0;

                    for (int column = 0; column < width; column++)
                        reconstruction += normal[m, column] * basis[i, first + column];

                    for (int k = 0; k <= l; k++)
                        harmonicFromMoment[first + m, k, i] = scale * reconstruction * legendre[l, k];
                }
            }
        }

        for (int channel = 0; channel < channels; channel++)
        {
            for (int k = 0; k <= ORDER; k++)
            {
                for (int group = 0; group < GROUPS; group++)
                {
                    int i = group * 4;
                    momentToHarmonic[channel * MOMENTS + k * GROUPS + group] = new double4(
                        harmonicFromMoment[channel, k, i],
                        harmonicFromMoment[channel, k, i + 1],
                        harmonicFromMoment[channel, k, i + 2],
                        harmonicFromMoment[channel, k, i + 3]
                    );
                }
            }
        }
    }

    static double[,] LegendreMonomials()
    {
        var coefficients = new double[ORDER + 1, ORDER + 1];
        coefficients[0, 0] = 1.0;
        coefficients[1, 1] = 1.0;

        // (n + 1) P_(n+1) = (2n + 1) t P_n - n P_(n-1)
        for (int n = 1; n < ORDER; n++)
        {
            for (int k = 0; k <= n + 1; k++)
            {
                double raised = k > 0 ? (2.0 * n + 1.0) * coefficients[n, k - 1] : 0.0;
                coefficients[n + 1, k] = (raised - n * coefficients[n - 1, k]) / (n + 1.0);
            }
        }

        return coefficients;
    }

    static void Invert(double[,] matrix, int size)
    {
        var inverse = new double[size, size];

        for (int i = 0; i < size; i++)
            inverse[i, i] = 1.0;

        for (int column = 0; column < size; column++)
        {
            int pivot = column;

            for (int row = column + 1; row < size; row++)
            {
                if (math.abs(matrix[row, column]) > math.abs(matrix[pivot, column]))
                    pivot = row;
            }

            for (int k = 0; k < size; k++)
            {
                (matrix[column, k], matrix[pivot, k]) = (matrix[pivot, k], matrix[column, k]);
                (inverse[column, k], inverse[pivot, k]) = (inverse[pivot, k], inverse[column, k]);
            }

            double reciprocal = 1.0 / matrix[column, column];

            for (int k = 0; k < size; k++)
            {
                matrix[column, k] *= reciprocal;
                inverse[column, k] *= reciprocal;
            }

            for (int row = 0; row < size; row++)
            {
                double factor = matrix[row, column];

                if (row == column || factor == 0.0)
                    continue;

                for (int k = 0; k < size; k++)
                {
                    matrix[row, k] -= factor * matrix[column, k];
                    inverse[row, k] -= factor * inverse[column, k];
                }
            }
        }

        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
                matrix[row, column] = inverse[row, column];
        }
    }
}
