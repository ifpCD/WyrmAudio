WyrmAudio is an under-development modern game audio framework and a spatial audio engine built atop of Steam Audio and Unity DOTS.  

Features:
* Frequency-Dependent 10th-Order Ambisonic Generation, each of the three bands at its own order
* Binaural 10th-Order Rendering with per-order magnitude-least-squares HRTFs over a 240-point spherical t-design, one convolution per source
* Mesh Soundfield Projection based on [Wang & Ramamoorthi Approach](https://cseweb.ucsd.edu/~ravir/ash.pdf)
* Subscription-Based Occlusion System
* Portal/Room Path Finding System
* Audio Source Pooling
* Extension-Based API
* Phonon's HRTF, Air Absorption, Directivity, Occlusion, Transmission, and Reflections
* Phonon's Transform Access and Interop Optimizations
* Extensive Runtime Visualization and Debugging Tools, including Ambisonic Field Visualization
* Asynchronous Multithreaded Data-Oriented Zero-Allocation Update Loop compiled with Burst LLVM