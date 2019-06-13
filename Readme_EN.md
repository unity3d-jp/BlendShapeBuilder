# Blend Shape Builder & Vertex Tweaker


![demo](https://user-images.githubusercontent.com/1488611/34981308-76511248-faea-11e7-8985-b8fe0e957035.gif)

This is a tool for building blend shapes in Unity. In addition to compositing existing models, it's also possible to edit vertices in Unity to create blend shapes.  
There are also features for things like creating special blend shape targets made from composites of vertices or vectors, or exporting existing blend shapes as a separate Mesh for each target.   

You will need Unity 2017.1 or later, Windows (64 bit) or Mac (**Graphics API of D3D11 and later**). 
(Please note that if your target platform is not standalone, D3D9 will limit functions and may not work properly.) 



## How to Use 

- Import [[BlendShapeBuilder.unitypackage](https://github.com/unity3d-jp/BlendShapeBuilder/releases/download/20190425/BlendShapeBuilder.unitypackage)to your project.
- For Unity 2018.3 and later, you can also import this repository directly. Open Packages/manifest.json of your project in the text editor and add it after "dependencies".
  > "com.utj.blendshapebuilder": "https://github.com/unity3d-jp/BlendShapeBuilder.git",
  
  -"Blend Shape Builder" and "Blend Shape Inspector", "Vertex Tweaker" will be added to the Window menu. 
  Blend Shape Builder is a tool to author blend shapes. Vertex Tweaker is for editing vertices. Blend Shape Inspector is for looking up or getting data from existing blend shapes. 


## Blend Shape Builder
When you select an object with MeshRenderer or SkinnedMeshRenderer with the Blend Shape Builder window open, the "Add BlendShapeBuilder" button will pop up so you can add it as a component. 
![BlendShapeBuilder](https://user-images.githubusercontent.com/1488611/34981508-05fb5fb6-faeb-11e7-9204-4aabd4c58543.png)

The object targeted by Blend Shape will be set as a GameObject with either MeshRenderer or SkinnedMeshRenderer. (The number of vertices must match)
-When you push the "Find Targets" button, it will find a model with the same number of vertices as the Base Mesh and select it. 

-Select objects that will be the morph target in "BlendShapes". 
  -If you want to target existing meshes, just drag and drop them. 
When you drag obejects to "▼ BlendShapes", a BlendShape will be spawned for each object. 
When you drag and drop and object to each BlendShape's fold (as in "▼ NewBlendShape0" shown above) the BlendShape will be registered as frames. 
 
  -By using the "+" button, a mesh will also be created in additon to the frames. 
  
  -If you click "Edit", you can edit the frame's mesh. (It will open the Vertex Tweaker, which we will talk about more below.) 
  
  -V,N,T is short for Vertex, Normal, Tangent and you can set whether to include them. For example, if you only select Normal, the vertices will not move and only the normal vectors will be changed for the BlendShape. 
  
  
  -You can update the current Mesh directly by clicking "Update Mesh". 
  "By clicking "Generate New Asset", you can export the mesh as a new asset without changing the current mesh. 
  When "Preserve Existing BlendShapes" is selected, it will carry the mesh over while adding blendshapes that were already in the mesh. (If there is already a BlendShape with the same name, it will be replaced with the newer one.) 
 


## Vertex Tweaker

This is a tool for editing vertices. It also supports editing meshes that have been skinned, and making minor changes to models in general (not just blend shapes). However, you can only move the vertices and will not be able to add/reduce the number of them. (In other words, you cannot change the topology) 

-Edit-> Move, Rotate, Scale is as implied, a mode that allows you to move, rotate, and scale vertices.  

-Edit-> Assign is for inputting values. You can mask depending on the XYZ axis. For example, you can select the bottom half of a sphere and select only Y and type in 0. This will make it a half sphere. 

-Edit-> Projection is a mode that allows you to project vertices on to other models. Project rays from each vertex and move the models accordingly. You can morph models that do not have the same number of vertices. 

- Select is for selecting vertices. Rectangle, roping, and brush selection are available, as well as Connected, which allows you to select sets of vertices that are connected. Edge and Hole also allow you to select vertices that are on the edges of polygons. 

-When you set the direction in Misc-> Mirroring, mirroring will be enabled. (Your model must be symmetric) 
-Misc-> Normals and Tangents are options to re-calculate the normal vectors and tangents. If there is a vector that you edited manually and you would like to preserve it, you will have to change to "Manual" to disable the automatic calculation. If this is not the case, there will be no problem with leaving it with the default settings. 

-Holding the Shift key while selecting is addition and holding the Ctrl key while selecting is subtraction. 

## Notes

-Editing imported meshes. 
    Meshes will be re-spawned each time the project is opened if it is imported from a fbx file. Therefore, edits on the imported meshes will not be saved if you reopen the project. 
    To solve this issue, enable "Add BlendShapeBuilder" for GameObjects with imported meshes, then create a copy of the original mesh and replace it with the MeshRenderer or SkinnedMeshRenderer mesh. 
    Blend Shape Builder's "Generate New Asset" and Vertex Tweaker's "Export-> Export .asset" commands will also export the model you are editing as an independent asset. If you do not want to change the original mesh, save it as a different asset with these command and edit that version instead. 
    
- When the number of vertices on the DCC tool does not match in Unity
  The Blend Shape target must have the same number of vertices as the original model. However, due to conversion, sometimes the number of vertices will not match on Unity after importing. When this happens, the following may occur: 
  -Uneven vectors (= hard edeges) 
  -The UV will be different for each model and there will be uneven UVs. 
  -Different sides having different material. 
  
  For example, to avoid this in Metasequioa, set the smoothing angle to 180 to avoid hard edges, and use the same UV for all models (Unity's Blend Shape does not account for UV changes). You must also only set one type of material per object. 
  
## Related Tools
- [NormalPainter](https://github.com/unity3d-jp/NormalPainter) - Edit vectors with this tool. 
- [FbxExporter](https://github.com/unity3d-jp/FbxExporter) - Export meshes in fbx format. It also supports skinning and blend shapes. 

## License
[MIT](LICENSE.txt)
