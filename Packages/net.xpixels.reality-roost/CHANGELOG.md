\# Changelog



\## \[0.6.0] - 2026-10-30



This update adds multiplayer buttons and grabbable objects. Add the prefabs to your scene and configure their actions in the Inspector.



\### Added



\#### Networked Button prefab

\- A button users can push with their hand or controller.

\- Can be configured to work once or be pressed repeatedly.

\- Actions can be assigned in the Inspector:

&#x20; - Select: runs when the user first touches the button.

&#x20; - Deselect: runs when the user removes their hand.

&#x20; - Activate: runs when the button is fully pushed down.



\#### Networked Grabbable prefab

\- An object users can pick up, move, and release.

\- Other players can see the object moving.

\- Actions can be assigned when the object is grabbed, released,

&#x20; or activated using the controller’s Activate input.

