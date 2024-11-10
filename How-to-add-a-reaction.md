# How to add a new reaction?

1. Install Guassians-to-blender from this source: https://github.com/eecheve/Gaussian-2-Blender
2. Make a copy of the SDF file of the reaction
3. Place the reaction SDF file and the SDFTruncate.py in the same new empty folder, such that the script doesn't mess up other files.
4. Run the Python script to truncate sdf file to meet the requirement of the gaussian-2-blender.
5. Run Gaussian-2-blender and set the "input names" as the truncated sdf file (or select all the files if you want to do batch process).
6. Set Model type to be "Balls and Stick" and Output type to be ".fbx".
   TODO: lower gamma corrections of the fbx file.
7. Convert the file!
8. Place the generated fbx file into the folder "AORA-UIToolKit\Assets\Resources"
9. Place the ORIGINAL sdf file into the folder "AORA-UIToolKit\Assets\StreamingAssets"
10. Locate the reactions.json file (Watch out for the real json file, not the meta file)
11. Add a the reaction into the array (within the [] brackets) as this format:
    {
    "category": "Conformational Change",
    "reactionName": "Butane (Anti to Gauche)",
    "filename": "butane_eclipsed",
    "transitionState": -1,
    "description": "This trajectory shows a simulation of butane with enough energy to achieve the transition state (eclipsed geometry) for rotation around the C2-C3 bond. This transition state connects the lower energy anti conformation with the higher energy gauche conformation. Notice that during the trajectory there is rotation of both the C2-C3 bond and the C1-C2 bond, but not at the exact same time."
    },
12. Run run the Unity to check if the reaction has been added.
13. Re compile the app!
