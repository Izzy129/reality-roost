Hi 

First! Thanks to bought the kit :) Really!

I hope it will give you satisfaction. But please, no matter the opinion you have 
(i prefried good, but i play the game), please take 5 minutes of your time to leave a 
review and put the stars that seem right to you... That really help us.

But pity not to use the review as weir anger or to request support. For that use : 
    
black.creepy.cat@gmail.com

Why ? Just because i check my mail more than the unity store reviews... 

-------------------------------------------------------------------------------------
Here are a series of questions and answers about the kit (I love talking to myself) :
-------------------------------------------------------------------------------------

---------------------
About the rendering : 
---------------------

To get the same as the video, you need to install the package post processing 
from the package manager window. Define your build project to LINEAR GAMMA 
and DEFFERED RENDERING. And use the fx profile i made for you!

https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html



-------------------------------------------
"I want this product working with URP/HDRP"
-------------------------------------------

All Unity package can be converted! If the publisher use Unity Standard Shaders, good news! It's the case :)

Follow this excellent Unity tutorial to make this package working on HDRP/URP : 
https://www.youtube.com/watch?v=VD5Qr4Rt7-Q

Check all this guy videos! Very good channel...



----------------------------------------------
« When I launch the demo, I can’t walk? Why?"
----------------------------------------------

- Keep calm :) I don’t include the controllers package into my pack, because they are generator of problem 
  when unity is updated. Here is the step to do : Include the package 
	
  https://assetstore.unity.com/packages/essentials/asset-packs/standard-assets-for-unity-2018-4-32351

  Inside the package there is a character controler prefab, dDrop it into the scene, and next remove the default scene camera.



--------------------------------
"Those doors did not open? Why?"
--------------------------------

- Keep calm :) Do not write a crappy review now :) You just have to put the door game objects 
  to static = off, to play with the doors during runtime (do not forget to re-check it for lightmap
  it's the pain in the ass of this methode i guess, check/uncheck all the time the static objects.
  I can not tell you the number of times, after two hours of lightmap, i realized that the doors were
  in static = off...)
  
  

--------------------------------------------------------------
"It's cool! But i want to do my hown creepy/darky textures?"
--------------------------------------------------------------
On this issue, I would not say that this will be super easy and fast, only one person who 
knows more or less photoshop can pass the race about the re-creation of textures, to make a good
adaptation for your project.

But I sincerely know that this is possible! and to help you in this direction :) I provide the 
flat textures (_Flat.png), and if you use them in photoshop + the normal maps (_Norm.png)
it will be possible easily recreate all the layers and then apply the effect/textures you want. 
choice.

It's a bit of work, yes! But it's possible. The kit has been made to this direction.

Note : Do not panic, i do not plan to move/modify/scale the position of the elements 
under the textures (i say that if you want to batch your hown), but i think add some 
new texture in the futur.


----------------------------------------------------------------------------------------------
"Creepy Cat! you suxx so much again! why do not provide texture in photoshop files? directly?"
----------------------------------------------------------------------------------------------
It's just because i don't do them under photoshop! but directly under my 3D software... To get good
diffuse/normal/ao/metal maps, i have my hown generation method, based on my experience. 

My personnal kitchen if you prefer :) That's why I did not have files with layers. 

But a motivated team that will use my kit as a graphic base, will be able to easily re-custom all the kit. 


-------------------------------------------------------
"I want the same video rendering!! Why i don't see it!"
-------------------------------------------------------
- The video has been recorded with lightmapping and unity post processing stack effects, please follow those links to get informations :

- Lightmapping : https://docs.unity3d.com/Manual/Lightmappers.html
- Post Processing : https://docs.unity3d.com/Packages/com.unity.postprocessing@3.1/manual/index.html


---------------------------------------------------------------------------
"Why there is a "MouseLook" error when i import your package with Occulus!"
---------------------------------------------------------------------------
- It's just a name conflict :) Just remove the component : MouseLook on the camera and delete the script Common Scripts => MouseLook


---------------------------------------------------------------------
"Hell! I compute lightmap but my rendering is black or/and glitched?"
---------------------------------------------------------------------

- Don't panic, it's a unity problem with lightmap, to fix it : 
  Clear the GI cache first, and recompute lightmap.

- Open the Window => Rendering => "Lightning"  and click => "Generate Lightning" and let the computer compute...


-------------------------------------------------------------------------------------
"Oh my god! there is a missing object! (There is always a missing object in a kit...)
-------------------------------------------------------------------------------------
To adapt a missing object to your kit, feel free to serve you some parts of textures!

Indeed, the kit is made with the rules of the modular design, and the textures also go
to this direction, it is possible, for example, with the texture: 

Wall_Atlas_07_dif.png 

To create (for those who know a 3D software) a calculation server ? or whatever
you want/need? just stay within each of textures and make your object based on them.

Wall_Atlas_01_Flat.png

By example can be used to create other specials missing walls for your project! 

Because yes!, create a video game, takes a bit of work .... 

To believe a kit solves all your game design issues for $ 30, and without working, is a heresy :)

A kit is a starting base! I will try one day to make a video to explain an example of 
re-using of the textures to create a needed/missing object.

The mesh "Energy_Ring_01.fbx" is a pure example of my previous explains, easily made
with the kit textures, same for the mesh "Tank_Base_03.fbx" etc...


And one time again :) Thanks to bought the kit :)


																						
																						
																						
																						
													Creepy Cat
























																																																																																																									