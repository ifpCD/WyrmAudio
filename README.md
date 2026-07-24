# Hello!
**WyrmAudio** is an audio middleware and occlusion/propagation engine written in C# that uses modified **Steam Audio**, **Unity's Burst and Jobs** technologies to bring state of the art performance and spatial effects, wrapped into a modern and opinionated API. 

## API Example
### One Shotting
```cs
WyrmMixer.Animated.Footsteps.Play(
    WyrmSound.FootstepsRockWalk,
    track: transform
);
```
### Borrowing
```cs
if (WyrmMixer.Objects.Torches.TryBorrow(out IWyrmSource source))
{
    source.TrackedTransform = transform;
    source.PlayOneShot(WyrmSound.Extinguish);
    source.Return(); // If you borrow, you have to return when done.
}
```

## Thesis
Game Developers often want a drop-in solution to:
1. Create the sense of directionality that's naturally perceived by humans.  
2. Create the sense of occlusion to elevate the notion of visibility.  
3. Create the sense of propagation to carry the notion of divided spaces.

Whilst all of these concepts have been mathematically solved for decades, many frictions persist:
1. **Spatial Engines** are often developed separately from game engines, creating unnecessary integration complexity. This separation often results in duplicated state management, additional synchronization layers, and reduced opportunities for engine-level optimizations.  
2.  In **Unity's** case, conventionally the **Spatial Engine** must receive the state (positions, source configuration) via constant wasteful interop (**Unity C++** -> **Scripting C#** -> **Spatial Engine**).
3. Spatial Engines often times focus on generalism/realism instead of artistic intent, limiting what the game developer can do.

## Steam Audio Issues
Issue 1: Performance   
Every frame - every **SteamAudioSource Component** does 39 interop calls:  
31 to send every spatializer value.  
8 to retrieve/send positional/directional data (origin, ahead, up, right).  
Why it's bad:  
**C#** <-> **C++** interop drastically impacts framerate.  
Anecdotal testing shows 0.3-0.4ms wasted cpu cost at 500 sources (9800X3D).  
Note: It's very easy to reduce 31 calls by caching last values in C# and only calling when marked as dirty.  

Issue 2: Propagation  
**Propagation** (called **Path Effect**) uses **A\*/Djikstra** algorithms in order to calculate the distance and the direction of approach from the source to the listener through open space. It optionally allows for runtime probe-to-probe raycasting for propagation occlusion.  
Why it's bad:  
Probes limit the developer's ability to dynamically close off space, instead forcing them to rely on path validation, which is expensive and causes distinct clasp sound or unnatural shift in propagation direction before dynamic geometry closes the path. Propagation is effectively unusable in the current state.  

Issue 3: Occlusion  
**Occlusion** is driven mainly via Quasi-Random (Halton Sequence) sphere of samples.  
Why it's bad:  
Every sample in the volume costs two raycasts:  
One from the sample to the listener, another from the sample to the origin of the source to check if that sample needs to be discarded.  
Whilst such raycasting option is necessary for generic objects, a more in depth option must exist to allow the developer to customize sample clouds dynamically to reduce sample discard rate and subsequently more accurate occlusion.  


## Performance Solutions
**WyrmAudio** aggresively utilizes **Unity's Burst/DOTS**, **Multi-Threading**, Audio Source GameObject pooling, and unsafe pointer passing to:
1. Offload Unity's Main Thread from in-engine, custom Occlusion/Propagation algorithms.
2. Operate on Native and Densely-Packed Arrays of Active Audio Source data, which is then passed to Phonon C via a single batched call containing pointers to said arrays, massively reducing interop cost every frame.
3. Increase game performance passively by reducing arbitrary GameObject Main Thread instantiation/destruction cost.  
An example is a humanoid character that has an Audio Source for feet, voice, and a weapon - 6 Component initialization reduction (Steam Audio Source and Audio Source); instead, an arbitrary component either asks WyrmPool (through mixer group extension) to one shot play at a tracked transform or static position, or borrows it and owns the lifecycle for the duration.
4. Facilitate Zero Post-Initialization Allocations (Unless you force the pool controller to create more sources on runtime).  

## Occlusion Solution  
Instead of dedicating rays to an audio source, allow the audio source to optionally subscribe to the occlusion rate of a predefined cloud of samples that requires no discard check raycasting.  
The predefined cloud of samples is tied to a transform of a central gameobject (up to the game developer)
Furthermore, allow the game developer to create LODs that reduce sample amount the further the origin gets.

## Propagation Solution  
#### Room/Portal Path Finding. 
Every frame a room bounds check is made for listener and each active source. (to be fniished)

## State of the repository
WyrmAudio has initially been developed very quickly with the intent to prototype the viability of all the highlighted technologies/design patterns to allow for a clean and hyper-optimized Unity-Centric Audio Package, but also to provide resources to implement highlighted occlusion and propagation algorithms in other engines.

It can be broken down into 4 distinct systems:  
1. **WyrmPoolController**: Multi-Threaded Contigious Native Array Occlusion/Propagation Simulation and pointer pass to Phonon C.  
2. Collection of Burst Parallel Jobs to offload Transform Access/Occlusion/Propagation operations.  
3. **WyrmMixer**: Opinionated API that uses C# auto-generation to provide global access to mixer groups and pooled audio sources.  
4. **WyrmSoundBank** A container for audio clip with built-in shuffling with similar C# auto-generation.  

At this stage most of performance testing, research and "hard work" is done.  
All that is left is designing modular architecture, implementing swappable quality occlusion/propagation algorithms, improving Phonon C API further, and designing comprehensive editor toolkit.