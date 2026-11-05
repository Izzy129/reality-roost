# Train Journey

Open `Assets/_RR_Development/Samples/Demo Scenes/RR_Demo_TrainJourney.unity` and select **World Rail / Train Journey**. In **Destinations**, add a slot for each visit, drag a Unity scene into **Scene**, and set **Travel Seconds**. Reorder slots or repeat a scene as needed. The route starts and ends at GrandStation automatically. Winter, Mojave, and Alien are the starting playlist.

The Inspector enables selected scenes in the project's global and active build scene lists. Build the same saved route for the host and clients. Use unique scene filenames for network loading. The SDK starts the experience through `RR_Boot`.

Use ordinary environment scenes that support additive loading. For a new destination, place the track at the scene origin facing positive Z. The train keeps its passenger camera and listener, moves scenery past the car, and uses the destination's lighting. Networked hierarchies keep their existing transforms and network ownership. Scene scripts still run their own startup logic.

Inside each tunnel the ride retires and unloads the previous environment, asynchronously loads the next scene, and spreads scenery activation across frames. The host waits for connected clients before leaving the tunnel. Large individual Unity objects can still take more than one frame's budget.

The sample destination scenes live in `Samples/Demo Scenes`, and their artwork lives in `Samples/Demo Assets`. To add your own destination, drag its existing scene into the Inspector playlist from anywhere in the project. Unity uses the assets already referenced by that scene.

The train stays anchored to Roost Rails. Its cabin remains open outdoors and closes on return to the station. `WorldRailLook` applies HDR, ACES grading, and restrained bloom.
