﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleJSON;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public static class CellPackLoader
{
	public static int current_color;
	public static List<Vector3> ColorsPalette;
	public static List<Vector3> ColorsPalette2;
	public static Dictionary<int,List<int>> usedColors;
	private static bool use_rigid_body = true;
    
    public static string GetPath(List<string> path)
    {
        var value = path[0];

        if (value.Count() > 1)
        {
            for (var i = 1; i < path.Count; i++)
            {
                value += "." + path[i];
            }
        }

        return value;
    }

    //static void BuildHierachy2(JSONNode resultData)
    //{
    //    var component = PersistantSettings.Instance;
    //    component.SceneHierachy.Clear();
        

    //    var currentPath = new List<string>();

    //    //currentPath.Add(resultData["recipe"]["name"]);
    //    //component.hierachy.Add(new Node(currentPath.Last()));

    //    if (resultData["cytoplasme"] != null)
    //    {
    //        currentPath.Add("cytoplasm");
    //        component.SceneHierachy.Add(new PersistantSettings.Node("cytoplasm", GetPath(currentPath)));

    //        var ingredients = resultData["cytoplasme"];

    //        for (int j = 0; j < ingredients["ingredients"].Count; j++)
    //        {
    //            string iname = ingredients["ingredients"][j]["name"];

    //            if (!SceneManager.Instance.ProteinIngredientNames.Contains("cytoplasme" + "_" + iname)) continue;
    //            currentPath.Add(iname);
    //            component.SceneHierachy.Add(new PersistantSettings.Node(iname, GetPath(currentPath)));
    //            currentPath.Remove(currentPath.Last());

    //        }

    //        currentPath.Remove(currentPath.Last());
    //    }

    //    for (int i = 0; i < resultData["compartments"].Count; i++)
    //    {
    //        var surfaceName = "surface" + i;
    //        var interiorName = "interior" + i;

    //        var compartment = resultData["compartments"].GetKey(i);

    //        currentPath.Add(compartment);
    //        component.SceneHierachy.Add(new PersistantSettings.Node(compartment, GetPath(currentPath)));

    //        //*****//

    //        var surface = "surface";
    //        var ingredients = resultData["compartments"][i][surface];

    //        if (ingredients["ingredients"].Count > 0)
    //        {
    //            currentPath.Add(surface);
    //            component.SceneHierachy.Add(new PersistantSettings.Node(surface, GetPath(currentPath)));

    //            for (int j = 0; j < ingredients["ingredients"].Count; j++)
    //            {
    //                string iname = ingredients["ingredients"][j]["name"];
    //                if (!SceneManager.Instance.ProteinIngredientNames.Contains(surfaceName + "_" + iname)) continue;
    //                currentPath.Add(iname);
    //                component.SceneHierachy.Add(new PersistantSettings.Node(iname, GetPath(currentPath)));
    //                currentPath.Remove(currentPath.Last());
    //            }

    //            currentPath.Remove(currentPath.Last());
    //        }

    //        //*****//

    //        var interior = "interior";
    //        ingredients = resultData["compartments"][i][interior];

    //        if (ingredients["ingredients"].Count > 0)
    //        {
    //            currentPath.Add(interior);
    //            component.SceneHierachy.Add(new PersistantSettings.Node(interior, GetPath(currentPath)));

    //            for (int j = 0; j < ingredients["ingredients"].Count; j++)
    //            {
    //                string iname = ingredients["ingredients"][j]["name"];
    //                if (!SceneManager.Instance.ProteinIngredientNames.Contains(interiorName + "_" + iname)) continue;
    //                currentPath.Add(iname);
    //                component.SceneHierachy.Add(new PersistantSettings.Node(iname, GetPath(currentPath)));
    //                currentPath.Remove(currentPath.Last());
    //            }

    //            currentPath.Remove(currentPath.Last());
    //        }

    //        currentPath.Remove(currentPath.Last());
    //    }

    //    int a = 0;

    //    //foreach (var node in component.hierachy)
    //    //{
    //    //    Debug.Log(" path: " + node.path);
    //    //}
    //}

    public static void LoadCellPackResults(bool load=true)
    {
        #if UNITY_EDITOR
		Debug.Log("Loading");
        var directory = "";

        if (string.IsNullOrEmpty(PersistantSettings.Instance.LastSceneLoaded) || !Directory.Exists(Path.GetDirectoryName(PersistantSettings.Instance.LastSceneLoaded)))
        {
            directory = Application.dataPath;
        }
        else
        {
            directory = Path.GetDirectoryName(PersistantSettings.Instance.LastSceneLoaded);
        }

        var path = EditorUtility.OpenFilePanel("Select .cpr", directory, "cpr");
        if (string.IsNullOrEmpty(path)) return;
        
        PersistantSettings.Instance.LastSceneLoaded = path;
        LoadIngredients(path);

        Debug.Log("*****");
        Debug.Log("Total protein atoms number: " + SceneManager.Get.TotalNumProteinAtoms);

        // Upload scene data to the GPU
        SceneManager.Get.UploadAllData();
        #endif
    }

    public static void LoadIngredients(string recipePath)
    {
        Debug.Log("*****");
        Debug.Log("Loading scene: " + recipePath);
        
        var cellPackSceneJsonPath = recipePath;//Application.dataPath + "/../Data/HIV/cellPACK/BloodHIV1.0_mixed_fixed_nc1.json";
        if (!File.Exists(cellPackSceneJsonPath)) throw new Exception("No file found at: " + cellPackSceneJsonPath);

        var resultData = MyUtility.ParseJson(cellPackSceneJsonPath);

        //we can traverse the json dictionary and gather ingredient source (PDB,center), sphereTree, instance.geometry if we want.
        //the recipe is optional as it will gave more information than just the result file.

        //idea: use secondary color scheme for compartments, and analogous color for ingredient from the recipe baseColor
        current_color = 0;
        //first grab the total number of object
        int nIngredients = 0;
        if (resultData["cytoplasme"] != null) nIngredients += resultData["cytoplasme"]["ingredients"].Count;

        for (int i = 0; i < resultData["compartments"].Count; i++)
        {
            nIngredients += resultData["compartments"][i]["interior"]["ingredients"].Count;
            nIngredients += resultData["compartments"][i]["surface"]["ingredients"].Count;
        }
        //generate the palette
        //ColorsPalette   = ColorGenerator.Generate(nIngredients).Skip(2).ToList(); 
        ColorsPalette = ColorGenerator.Generate(8).Skip(2).ToList();//.Skip(2).ToList();
        List<Vector3> startKmeans = new List<Vector3>(ColorsPalette);
        //paletteGenerator.initKmeans (startKmeans);

        usedColors = new Dictionary<int, List<int>>();
        ColorsPalette2 = ColorPaletteGenerator.generate(
                6, // Colors
                ColorPaletteGenerator.testfunction,
                false, // Using Force Vector instead of k-Means
                50 // Steps (quality)
                );
        // Sort colors by differenciation first
        //ColorsPalette2 = paletteGenerator.diffSort(ColorsPalette2);
        //check if cytoplasme present

        Color baseColor = new Color(1.0f, 107.0f / 255.0f, 66.0f / 255.0f);
        if (resultData["cytoplasme"] != null)
        {
            usedColors.Add(current_color, new List<int>());
            baseColor = new Color(1.0f, 107.0f / 255.0f, 66.0f / 255.0f);
            AddRecipeIngredients(resultData["cytoplasme"]["ingredients"], baseColor, "root", "cytoplasme");
            current_color += 1;
        }

        for (int i = 0; i < resultData["compartments"].Count; i++)
        {
            var compartmentName = resultData["compartments"].GetKey(i);

            baseColor = new Color(148.0f / 255.0f, 66.0f / 255.0f, 255.0f / 255.0f);
            usedColors.Add(current_color, new List<int>());

            AddRecipeIngredients(resultData["compartments"][i]["interior"]["ingredients"], baseColor, "root", compartmentName, "interior");

            current_color += 1;
            baseColor = new Color(173.0f / 255.0f, 255.0f / 255.0f, 66.0f / 255.0f);
            usedColors.Add(current_color, new List<int>());

            AddRecipeIngredients(resultData["compartments"][i]["surface"]["ingredients"], baseColor, "root", compartmentName, "surface");
            current_color += 1;
        }
    }

	public static void AddRecipeIngredients(JSONNode recipeDictionary, Color baseColor, params string[] pathElements)
    {
		for (int j = 0; j < recipeDictionary.Count; j++)
		{
            if (recipeDictionary[j]["nbCurve"] != null)
            {
                AddCurveIngredients(recipeDictionary[j], pathElements);
            }
            else
            {
                AddProteinIngredient(recipeDictionary[j], pathElements);
            }
        }
	}

    public static void AddProteinIngredient(JSONNode ingredientDictionary, params string[] pathElements)
    {
        var name = ingredientDictionary["name"];
        var path = MyUtility.GetUrlPath(pathElements.ToList(), name);
        var biomt = (bool)ingredientDictionary["source"]["biomt"].AsBool;
        var center = (bool)ingredientDictionary["source"]["transform"]["center"].AsBool;
        var pdbName = ingredientDictionary["source"]["pdb"].Value.Replace(".pdb", "");
        
        List<Vector4> atomSpheres;
		List<Matrix4x4> biomtTransforms = new List<Matrix4x4>();
		Vector3 biomtCenter = Vector3.zero;
		bool containsACarbonOnly = false;
		bool oneLOD = false;

        if ((pdbName == "") || (pdbName == "null") || (pdbName == "None")||pdbName.StartsWith("EMDB"))
        {
            return;

            ////check for sphere file//information in the file. if not in file is it on disk ? on repo ?
            ////possibly read the actuall recipe definition ?
            ////check if bin exist
            //var filePath = PdbLoader.DefaultPdbDirectory + ingredientDictionary["name"] + ".bin";
            //if (File.Exists(filePath)){
            //	atomSpheres = new List<Vector4>();
            //	var points = MyUtility.ReadBytesAsFloats(filePath);
            //	for (var i = 0; i < points.Length; i += 4) {
            //		var currentAtom = new Vector4 (points [i], points [i + 1], points [i + 2], points [i + 3]);
            //		atomSpheres.Add (currentAtom);
            //	}
            //	containsACarbonOnly = true;
            //	oneLOD = true;
            //}
            //else if (ingredientDictionary ["radii"] != null) {
            //	atomSpheres = MyUtility.gatherSphereTree(ingredientDictionary)[0];
            //	Debug.Log ("nbprim "+atomSpheres.Count.ToString());//one sphere
            //	oneLOD = true;
            //} else {
            //	float radius = 30.0f;
            //	if (name.Contains("dLDL"))
            //		radius = 108.08f;//or use the mesh? or make sphere from the mesh ?
            //	if (name.Contains("iLDL"))
            //		radius = 105.41f;//or use the mesh? or make sphere from the mesh ?
            //	atomSpheres = new List<Vector4>();
            //	atomSpheres.Add (new Vector4(0,0,0,radius));
            //	//No LOD since only one sphere
            //	oneLOD = true;
            //}
        }
        else
        {
			//if (pdbName.StartsWith("EMDB")) return;
			//if (pdbName.Contains("1PI7_1vpu_biounit")) return;//??
			// Load atom set from pdb file
			var atomSet = PdbLoader.LoadAtomSet(pdbName);
			
			// If the set is empty return
			if (atomSet.Count == 0) return;
		
			atomSpheres = AtomHelper.GetAtomSpheres(atomSet);
			containsACarbonOnly = AtomHelper.ContainsCarbonAlphaOnly(atomSet);
		}
		var centerPosition = AtomHelper.ComputeBounds(atomSpheres).center;       
		
		// Center atoms
		AtomHelper.OffsetSpheres(ref atomSpheres, centerPosition);
		
		// Compute bounds
		var bounds = AtomHelper.ComputeBounds(atomSpheres);

		biomtTransforms = biomt ? PdbLoader.LoadBiomtTransforms(pdbName) : new List<Matrix4x4>();
		biomtCenter = AtomHelper.GetBiomtCenter(biomtTransforms,centerPosition);

		//if (!pdbName.Contains("1TWT_1TWV")) return;
        
        // Disable biomts until loading problem is resolved
        //if (!biomt) return;
        

        // Get ingredient color
        // TODO: Move color palette code into dedicated function
        var cid = ColorPaletteGenerator.GetRandomUniqFromSample(current_color, usedColors[current_color]);
        usedColors[current_color].Add(cid);
        var sample = ColorPaletteGenerator.colorSamples[cid];
        var c = ColorPaletteGenerator.lab2rgb(sample) / 255.0f;
        var color = new Color(c[0], c[1], c[2]);

        // Define cluster decimation levels
		var clusterLevels = (containsACarbonOnly)
		? new List<float>() {0.85f, 0.25f, 0.1f}
            : new List<float>() {0.15f, 0.10f, 0.05f};
		if (oneLOD)
			clusterLevels = new List<float> () {1, 1, 1};
        // Add ingredient type
        //SceneManager.Instance.AddIngredient(name, bounds, atomSpheres, color);
	
		SceneManager.Get.AddProteinIngredient(path, bounds, atomSpheres, color, clusterLevels,oneLOD);
        int instanceCount = 0;
        
        for (int k = 0; k < ingredientDictionary["results"].Count; k++)
        {
            var p = ingredientDictionary["results"][k][0];
            var r = ingredientDictionary["results"][k][1];

            var position = new Vector3(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat);
            var rotation = new Quaternion(r[0].AsFloat, r[1].AsFloat, r[2].AsFloat, r[3].AsFloat);

            var mat = MyUtility.quaternion_matrix(rotation);
            var euler = MyUtility.euler_from_matrix(mat);
            rotation = MyUtility.MayaRotationToUnity(euler);

            if (!biomt)
            {
                // Find centered position
                if (!center) position += MyUtility.QuaternionTransform(rotation, centerPosition);
                SceneManager.Get.AddProteinInstance(path, position, rotation);
                instanceCount++;
            }
            else
            {
                foreach (var transform in biomtTransforms)
                {
					var biomteuler = MyUtility.euler_from_matrix(transform);
					var rotBiomt = MyUtility.MayaRotationToUnity(biomteuler);
					var offset = MyUtility.QuaternionTransform(rotBiomt,centerPosition);//Helper.RotationMatrixToQuaternion(matBiomt), GetCenter());
					var posBiomt = new Vector3(-transform.m03, transform.m13, transform.m23)+offset - biomtCenter;

					var biomtOffset = MyUtility.RotationMatrixToQuaternion(transform) * centerPosition;
					var biomtInstanceRot = rotation * rotBiomt;//Helper.RotationMatrixToQuaternion(transform);
					var biomtInstancePos = rotation * posBiomt + position;

					SceneManager.Get.AddProteinInstance(path, biomtInstancePos, biomtInstanceRot);
                    instanceCount++;
                }
            }
        }

        SceneManager.Get.AddIngredientProperties(atomSpheres.Count, instanceCount);

        Debug.Log("*****");
        Debug.Log("Added ingredient: " + path);

        //if (isCarbonAlphaOnly) Debug.Log("Alpha-carbons only");
        //Debug.Log("Pdb name: " + pdbName + " *** " + "Num atoms: " + atomSet.Count + " *** " + "Num instances: " + instanceCount + " *** " + "Total atom count: " + atomSet.Count * instanceCount);
    }

    public static void AddCurveIngredients(JSONNode ingredientDictionary , params string[] pathElements)
    {
        var name = ingredientDictionary["name"];
        var path = MyUtility.GetUrlPath(pathElements.ToList(), name);
        var numCurves = ingredientDictionary["nbCurve"].AsInt;
        var pdbName = ingredientDictionary["source"]["pdb"].Value.Replace(".pdb", "");

        SceneManager.Get.AddCurveIngredient(path, pdbName);
        
        for (int i = 0; i < numCurves; i++)
        {
            //if (i < nCurve-10) continue;
            var controlPoints = new List<Vector4>();
            if (ingredientDictionary["curve" + i.ToString()].Count < 4) continue;

            for (int k = 0; k < ingredientDictionary["curve" + i.ToString()].Count; k++)
            {
                var p = ingredientDictionary["curve" + i.ToString()][k];
                controlPoints.Add(new Vector4(-p[0].AsFloat, p[1].AsFloat, p[2].AsFloat, 1));
            }

            SceneManager.Get.AddCurveIntance(path, controlPoints);
            //break;
        }

        Debug.Log("*****");
        Debug.Log("Added curve ingredient: " + path);
        Debug.Log("Num curves: " + numCurves);
    }
	
    public static void DebugMethod()
    {
        Debug.Log("Hello World");
    }


    public static void LoadMembrane()
    {
#if UNITY_EDITOR
        Debug.Log("Loading");
        var directory = "";

        if (string.IsNullOrEmpty(PersistantSettings.Instance.LastSceneLoaded) || !Directory.Exists(Path.GetDirectoryName(PersistantSettings.Instance.LastSceneLoaded)))
        {
            directory = Application.dataPath;
        }
        else
        {
            directory = Path.GetDirectoryName(PersistantSettings.Instance.LastSceneLoaded);
        }

        var path = EditorUtility.OpenFilePanel("Select .mbr", directory, "mbr");
        if (string.IsNullOrEmpty(path)) return;

        SceneManager.Get.AddMembrane(path, Vector3.zero, Quaternion.identity);

        // Upload scene data to the GPU
        SceneManager.Get.UploadAllData();
#endif
    }
}
