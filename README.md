# Real-Time Shading with Polyhedral Lights Using Silhouette Detection
This is my implementation of the techniques presented in my bachelor thesis [Real-Shading with Polyhedral Lights Using Silhouette Detection](http://bastian.urbach.one/rtswplusd). The implementation is a Unity Project (version 2021.1.13f1). To run it, open it in the Unity Editor, open one of the scenes in the "Example Scenes" folder and press "Play". To create a new light source, add one of the light source scripts (Scripts/Lights) to a game object and configure it to your liking. The script requires precomputed data in some format depending on the type of the light source. For example, a ConvexLTCLight requires a ConvexSilhouetteBSP. To create one, select Assets > Create > Convex Silhouette BSP. This will create a new asset in the project folder that you can select and configure. You can then assign it to the light source. Note that the polyhedral light sources currently only work in play mode and only in the deferred shading path of the builtin render pipeline. They also don't work for transparent objects or any other object that does not use deferred shading.
