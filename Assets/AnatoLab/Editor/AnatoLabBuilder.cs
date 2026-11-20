#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnatoLab.Editor
{
    public static class AnatoLabBuilder
    {
        const string Output = "Assets/AnatoLab/GeneratedFinal";
        static TMP_FontAsset font;
        static readonly Color ink = new Color(0.89f, 0.94f, 0.96f);
        static readonly Color muted = new Color(0.48f, 0.62f, 0.69f);
        static readonly Color accent = new Color(0.19f, 0.87f, 0.74f);
        static readonly Color panel = new Color(0.036f, 0.058f, 0.091f, 0.98f);
        static readonly Color line = new Color(0.12f, 0.20f, 0.25f);
        static readonly Color button = new Color(0.075f, 0.13f, 0.18f);

        sealed class Region
        {
            public string name, latin, area, description, function;
            public Vector3 direction;
            public Region(string n, string l, string a, string d, string f, Vector3 v) { name = n; latin = l; area = a; description = d; function = f; direction = v; }
        }

        [MenuItem("AnatoLab/Bangun Muscular Explorer %&m", false, 0)]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Keluar dari Play Mode sebelum membangun scene."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = AssetDatabase.GUIDToAssetPath("0f63650417efeab4ca65f3522b94dd99");
            if (string.IsNullOrEmpty(path)) path = AssetDatabase.FindAssets("Meshy_AI_Human_ecorche_full").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith(".glb"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Model Meshy tidak ditemukan di Assets/Models.");
            EnsureFolder("Assets/AnatoLab"); EnsureFolder(Output); EnsureFolder(Output + "/Meshes"); EnsureFolder(Output + "/Materials");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("Import TMP Essential Resources terlebih dahulu.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.53f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.23f, 0.29f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.12f, 0.16f);
            RenderSettings.fog = false;
            var instance = UnityEngine.Object.Instantiate(prefab); instance.name = "Source model (temporary)";
            var filters = instance.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0) throw new InvalidOperationException("Model tidak memiliki MeshFilter.");
            var bounds = filters[0].GetComponent<Renderer>().bounds;
            foreach (var filter in filters) bounds.Encapsulate(filter.GetComponent<Renderer>().bounds);
            var body = new GameObject("Human Muscular System - Surface Regions");
            var definitions = Definitions();
            var grouped = new Dictionary<string, List<Part>>();
            float scale = 2.0f / bounds.size.y;
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh; var positions = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
                var points = new Vector3[positions.Length]; var ns = new Vector3[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    points[i] = Quaternion.Euler(0, 180, 0) * ((filter.transform.TransformPoint(positions[i]) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * scale);
                    ns[i] = normals.Length == positions.Length ? Quaternion.Euler(0, 180, 0) * filter.transform.TransformDirection(normals[i]).normalized : Vector3.up;
                }
                var renderer = filter.GetComponent<Renderer>();
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var tri = mesh.GetTriangles(sub); var bins = new Dictionary<string, List<int>>();
                    for (int i = 0; i < tri.Length; i += 3)
                    {
                        var p = (points[tri[i]] + points[tri[i + 1]] + points[tri[i + 2]]) / 3f;
                        string key = Classify(p);
                        if (!bins.ContainsKey(key)) bins[key] = new List<int>();
                        bins[key].Add(tri[i]); bins[key].Add(tri[i + 1]); bins[key].Add(tri[i + 2]);
                    }
                    var sourceMaterial = renderer.sharedMaterials[Mathf.Min(sub, renderer.sharedMaterials.Length - 1)];
                    foreach (var bin in bins)
                    {
                        if (!grouped.ContainsKey(bin.Key)) grouped[bin.Key] = new List<Part>();
                        grouped[bin.Key].Add(new Part { vertices = points, normals = ns, uv = uv, triangles = bin.Value, material = sourceMaterial });
                    }
                }
            }
            var surfaceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var firstMaterial = filters[0].GetComponent<Renderer>().sharedMaterial;
            string textureKey = firstMaterial.GetTexturePropertyNames().FirstOrDefault(n => n.ToLowerInvariant().Contains("basecolor") || n == "_BaseMap" || n == "_MainTex");
            var texture = textureKey != null ? firstMaterial.GetTexture(textureKey) : firstMaterial.mainTexture;
            surfaceMaterial.SetTexture("_BaseMap", texture);
            surfaceMaterial.SetColor("_BaseColor", new Color(0.95f, 0.93f, 0.90f));
            surfaceMaterial.SetFloat("_Metallic", 0); surfaceMaterial.SetFloat("_Smoothness", 0.28f); surfaceMaterial.SetFloat("_Cull", 0);
            surfaceMaterial.EnableKeyword("_EMISSION"); surfaceMaterial.SetColor("_EmissionColor", Color.black);
            surfaceMaterial = SaveAsset(surfaceMaterial, Output + "/Materials/MuscleSurface.mat");
            foreach (var group in grouped)
            {
                var go = new GameObject(group.Key); go.layer = 8; go.transform.SetParent(body.transform);
                var data = go.AddComponent<OrganData>(); var def = definitions[group.Key];
                data.organName = def.name; data.latinName = def.latin; data.region = def.area; data.description = def.description; data.function = def.function; data.viewDirection = def.direction;
                int partIndex = 0;
                foreach (var part in group.Value)
                {
                    var child = new GameObject("Surface " + partIndex); child.layer = 8; child.transform.SetParent(go.transform, false);
                    var mesh = MakeMesh(part); mesh.name = group.Key + "_" + partIndex;
                    mesh = SaveAsset(mesh, Output + "/Meshes/" + mesh.name + ".asset");
                    child.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = child.AddComponent<MeshRenderer>(); r.sharedMaterial = surfaceMaterial; r.receiveShadows = true;
                    var hull = MakeHull(mesh); hull = SaveAsset(hull, Output + "/Meshes/" + mesh.name + "_Collider.asset");
                    var col = child.AddComponent<MeshCollider>(); col.sharedMesh = hull; col.convex = true;
                    partIndex++;
                }
            }
            UnityEngine.Object.DestroyImmediate(instance);
            var ghost = new Material(Shader.Find("AnatoLab/Ghost")); ghost.name = "XRay";
            ghost.SetColor("_BaseColor", new Color(0.20f, 0.65f, 0.72f, 0.12f)); ghost.renderQueue = 3000;
            ghost = SaveAsset(ghost, Output + "/Materials/XRay.mat");
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.018f, 0.030f, 0.050f);
            camera.fieldOfView = 35; camera.nearClipPlane = 0.03f; camera.farClipPlane = 100; camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            var orbit = cameraObject.AddComponent<OrbitCamera>(); orbit.Initialize(new Vector3(0, 1.02f, 0), 4.05f);
            var systems = new GameObject("AnatoLab Systems");
            var selector = systems.AddComponent<OrganSelector>(); selector.viewCamera = camera; selector.orbit = orbit; selector.ghostMaterial = ghost;
            var app = systems.AddComponent<AnatomyApp>(); app.selector = selector; app.orbit = orbit; app.font = font;
            BuildStage(); Light("Key - warm", new Vector3(38, -32, 0), new Color(1f, 0.88f, 0.76f), 2.2f);
            Light("Fill - cool", new Vector3(22, 145, 0), new Color(0.42f, 0.75f, 1f), 1.6f);
            Light("Rim", new Vector3(-25, -145, 0), new Color(0.2f, 1f, 0.85f), 1.1f);
            BuildUI(app);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            string scenePath = "Assets/AnatoLab/MuscularExplorer.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.productName = "AnatoLab - Human Muscular System";
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets(); Selection.activeGameObject = systems;
            Debug.Log("ANATOLAB READY | " + grouped.Count + " surface regions | " + scenePath + " | Source model preserved. Approximate regions, not segmented anatomical muscle meshes.");
            EditorUtility.DisplayDialog("AnatoLab siap", grouped.Count + " wilayah permukaan dibuat.\nScene: Assets/AnatoLab/MuscularExplorer.unity\n\nTekan Play untuk menguji. Batas wilayah merupakan perkiraan dari mesh gabungan.", "Lihat scene");
        }

        sealed class Part { public Vector3[] vertices, normals; public Vector2[] uv; public List<int> triangles; public Material material; }
        static Mesh MakeMesh(Part p)
        {
            var map = new Dictionary<int, int>(); var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            foreach (var old in p.triangles)
            {
                if (!map.TryGetValue(old, out int index)) { index = vertices.Count; map.Add(old, index); vertices.Add(p.vertices[old]); normals.Add(p.normals[old]); uv.Add(p.uv.Length > old ? p.uv[old] : Vector2.zero); }
                triangles.Add(index);
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }
        static Mesh MakeHull(Mesh surface)
        {
            var source = surface.vertices; int count = Mathf.Min(96, source.Length); var verts = new Vector3[count];
            for (int i = 0; i < count; i++) verts[i] = source[(int)((long)i * source.Length / count)];
            var triangles = new int[Mathf.Max(0, count - 2) * 3];
            for (int i = 0; i < count - 2; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            var mesh = new Mesh { vertices = verts, triangles = triangles }; mesh.RecalculateBounds(); return mesh;
        }
        static string Classify(Vector3 p)
        {
            float t = p.y / 2f; float x = Mathf.Abs(p.x); string side = p.x < 0 ? "R" : "L"; bool front = p.z < 0;
            if (t > 0.88f) return "head";
            if (t > 0.80f && x < 0.13f) return "neck";
            if (t > 0.69f && x > 0.15f) return "deltoid_" + side;
            if (x > 0.22f && t > 0.39f) return (t > 0.59f ? (front ? "biceps_" : "triceps_") : "forearm_") + side;
            if (x > 0.24f && t > 0.31f) return "hands";
            if (t > 0.66f) return front ? "chest" : "trapezius";
            if (t > 0.51f) return front ? (x < 0.10f ? "abdominal" : "oblique") : "latissimus";
            if (t > 0.42f) return front ? "pelvis" : "gluteal";
            if (t > 0.235f) return (front ? "quadriceps_" : "hamstrings_") + side;
            if (t > 0.055f) return (front ? "tibialis_" : "calf_") + side;
            return "feet";
        }
        static Dictionary<string, Region> Definitions()
        {
            var d = new Dictionary<string, Region>();
            void Add(string key, string name, string latin, string area, string desc, string function, bool back = false) { d[key] = new Region(name, latin, area, desc, function, back ? Vector3.forward : Vector3.back); }
            Add("head", "Wilayah kepala", "Regio capitis", "Kepala", "Wilayah ini mencakup permukaan kepala. Otot wajah berperan dalam ekspresi, sedangkan otot pengunyahan menggerakkan mandibula.\n\nModel ini tidak memisahkan otot wajah satu per satu.", "Ekspresi wajah dan gerakan pengunyahan.");
            Add("neck", "Wilayah leher", "Regio cervicalis", "Leher", "Leher mengandung beberapa kelompok otot, termasuk sternocleidomastoideus dan otot penstabil leher.\n\nSternocleidomastoideus membantu memutar kepala ke sisi berlawanan dan memiringkannya ke sisi yang sama.", "Gerak dan stabilisasi kepala serta leher.");
            Add("chest", "Dada", "Pectoralis major", "Toraks anterior", "Pectoralis major adalah otot berbentuk kipas pada dada bagian depan. Otot ini memiliki bagian klavikular dan sternokostal, dengan insersi pada humerus.\n\nWilayah pilihan mewakili permukaan dada; bukan batas serabut otot yang presisi.", "Adduksi dan rotasi medial lengan. Bagian klavikular membantu fleksi bahu.");
            Add("trapezius", "Punggung atas", "Trapezius", "Punggung posterior", "Trapezius merupakan otot luas dari daerah oksipital dan tulang belakang servikotorakal menuju klavikula serta skapula. Serabut atas, tengah, dan bawah memiliki arah tarikan yang berbeda.", "Elevasi, retraksi, depresi, dan rotasi ke atas skapula melalui kerja bagian-bagiannya.", true);
            Add("abdominal", "Perut depan", "Rectus abdominis", "Abdomen anterior", "Rectus abdominis adalah otot berpasangan pada dinding perut depan. Otot ini terbentang dari pubis menuju prosesus xiphoideus dan kartilago iga.\n\nGaris jaringan ikat melintang membentuk tampilan segmen pada permukaannya.", "Fleksi batang tubuh, kompresi abdomen, dan membantu stabilitas pelvis.");
            Add("oblique", "Perut samping", "Obliquus externus abdominis", "Abdomen lateral", "Obliquus externus merupakan lapisan superfisial dinding perut samping. Serabutnya umumnya berjalan ke bawah dan ke medial. Otot ini bekerja bersama lapisan otot abdomen lainnya.", "Kompresi abdomen, fleksi lateral, dan rotasi batang tubuh ke sisi berlawanan.");
            Add("latissimus", "Punggung tengah", "Latissimus dorsi", "Punggung posterior", "Latissimus dorsi adalah otot lebar pada bagian belakang batang tubuh. Otot ini menghubungkan wilayah torakolumbal dan pelvis dengan humerus.\n\nWilayah ini juga dapat mencakup permukaan otot punggung lain pada model gabungan.", "Ekstensi, adduksi, dan rotasi medial lengan pada sendi bahu.", true);
            Add("pelvis", "Wilayah panggul depan", "Regio pelvica anterior", "Pelvis", "Wilayah panggul depan merupakan daerah peralihan batang tubuh dan paha. Beberapa struktur di daerah ini merupakan tendon, fasia, serta otot yang lebih dalam.\n\nTidak semua otot panggul dapat dinilai dari permukaan model.", "Peralihan beban tubuh serta dukungan gerak panggul bersama otot sekitarnya.");
            Add("gluteal", "Bokong", "Gluteus maximus", "Pelvis posterior", "Gluteus maximus adalah otot superfisial besar pada bokong. Otot ini menghubungkan bagian posterior pelvis dengan femur serta traktus iliotibialis.", "Ekstensi dan rotasi lateral paha. Aktif saat bangkit, menaiki tangga, atau berlari.", true);
            Add("hands", "Wilayah tangan", "Regio manus", "Ekstremitas atas", "Tangan memiliki otot intrinsik serta tendon otot lengan bawah. Gerak jari membutuhkan koordinasi beberapa kelompok otot.\n\nMesh ini belum memisahkan setiap otot dan tendon tangan.", "Gerak halus jari, menggenggam, dan manipulasi objek.");
            Add("feet", "Wilayah kaki", "Regio pedis", "Ekstremitas bawah", "Kaki mengandung otot intrinsik, tendon, dan jaringan penopang lengkung kaki. Otot ekstrinsik dari tungkai turut menggerakkan kaki dan jari.\n\nWilayah pilihan bukan representasi satu otot tertentu.", "Menopang berat badan, menjaga lengkung kaki, dan membantu langkah.");
            foreach (string side in new[] { "L", "R" })
            {
                string suffix = side == "L" ? " kiri" : " kanan";
                Add("deltoid_" + side, "Bahu" + suffix, "Deltoideus", "Bahu", "Deltoideus menyelubungi sendi bahu. Otot ini terdiri atas bagian anterior, tengah, dan posterior yang berasal dari klavikula serta skapula, lalu berinsersi pada humerus.", "Bagian tengah melakukan abduksi bahu; bagian depan dan belakang membantu fleksi atau ekstensi serta rotasi lengan.");
                Add("biceps_" + side, "Lengan atas depan" + suffix, "Biceps brachii", "Lengan anterior", "Biceps brachii memiliki dua kepala yang berasal dari skapula dan berinsersi pada radius. Otot ini berada di permukaan lengan atas bagian depan.\n\nArea pilihan dapat mencakup otot di sekitarnya.", "Supinasi lengan bawah dan fleksi siku; juga membantu fleksi bahu.");
                Add("triceps_" + side, "Lengan atas belakang" + suffix, "Triceps brachii", "Lengan posterior", "Triceps brachii memiliki tiga kepala. Kepala panjang berasal dari skapula, sedangkan kepala lateral dan medial berasal dari humerus. Ketiganya menuju olekranon ulna.", "Ekstensi siku. Kepala panjang turut membantu ekstensi serta adduksi bahu.", true);
                Add("forearm_" + side, "Lengan bawah" + suffix, "Kelompok otot antebrachium", "Lengan bawah", "Lengan bawah berisi kelompok fleksor dan ekstensor pergelangan serta jari, juga otot pronasi dan supinasi.\n\nPilihan ini merupakan wilayah gabungan; tidak menunjuk satu otot individual.", "Menggerakkan pergelangan dan jari serta memutar lengan bawah.");
                Add("quadriceps_" + side, "Paha depan" + suffix, "Quadriceps femoris", "Paha anterior", "Quadriceps femoris terdiri atas rectus femoris, vastus lateralis, vastus medialis, dan vastus intermedius. Kelompok ini terhubung dengan patela dan tuberositas tibia melalui mekanisme ekstensor lutut.", "Ekstensi lutut. Rectus femoris juga membantu fleksi panggul.");
                Add("hamstrings_" + side, "Paha belakang" + suffix, "Kelompok hamstring", "Paha posterior", "Kelompok hamstring meliputi semitendinosus, semimembranosus, serta biceps femoris. Sebagian besar berasal dari tuberositas ischii dan menuju tungkai bawah. Kepala pendek biceps femoris berasal dari femur.", "Fleksi lutut; sebagian besar anggota juga membantu ekstensi panggul.", true);
                Add("tibialis_" + side, "Tungkai depan" + suffix, "Tibialis anterior", "Tungkai anterior", "Tibialis anterior terletak pada kompartemen depan tungkai. Tendonnya berjalan melewati bagian depan pergelangan menuju sisi medial kaki.\n\nPilihan menandai wilayah permukaan, termasuk struktur di dekatnya.", "Dorsifleksi dan inversi kaki; membantu mengontrol penurunan kaki saat berjalan.");
                Add("calf_" + side, "Betis" + suffix, "Gastrocnemius dan soleus", "Tungkai posterior", "Gastrocnemius membentuk bagian superfisial betis, sedangkan soleus berada lebih dalam. Keduanya bergabung pada tendon kalkaneus menuju tulang tumit.\n\nGastrocnemius melintasi sendi lutut dan pergelangan kaki.", "Plantarfleksi pergelangan kaki. Gastrocnemius turut membantu fleksi lutut.", true);
            }
            return d;
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/'); AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
        }
        static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(asset, existing); UnityEngine.Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static void Light(string name, Vector3 rotation, Color color, float intensity)
        {
            var go = new GameObject(name); go.transform.rotation = Quaternion.Euler(rotation); var light = go.AddComponent<UnityEngine.Light>(); light.type = LightType.Directional; light.color = color; light.intensity = intensity;
            light.shadows = name.StartsWith("Key") ? LightShadows.Soft : LightShadows.None;
        }
        static void BuildStage()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", new Color(0.025f, 0.055f, 0.075f)); mat.SetFloat("_Smoothness", 0.45f); mat = SaveAsset(mat, Output + "/Materials/Stage.mat");
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder); platform.name = "Display plinth"; platform.transform.position = new Vector3(0, -0.045f, 0); platform.transform.localScale = new Vector3(1.05f, 0.035f, 1.05f); UnityEngine.Object.DestroyImmediate(platform.GetComponent<Collider>()); platform.GetComponent<Renderer>().sharedMaterial = mat;
            var ringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); ringMat.SetColor("_BaseColor", new Color(0.12f, 0.63f, 0.58f)); ringMat = SaveAsset(ringMat, Output + "/Materials/Ring.mat");
            for (int k = 0; k < 2; k++)
            {
                var ring = new GameObject("Stage ring " + k).AddComponent<LineRenderer>(); ring.sharedMaterial = ringMat; ring.positionCount = 96; ring.loop = true; ring.startWidth = ring.endWidth = k == 0 ? 0.004f : 0.0015f;
                float radius = 0.51f + k * 0.12f; for (int i = 0; i < 96; i++) { float a = i * Mathf.PI * 2 / 96; ring.SetPosition(i, new Vector3(Mathf.Sin(a) * radius, -0.006f, Mathf.Cos(a) * radius)); }
            }
        }

        static TMP_Text Label(Transform parent, string name, string value, float size, Color color, float height, bool bold = false)
        {
            var text = UiKit.Text(name, parent, value, font, size, color); text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            var layout = text.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height; layout.minHeight = height; return text;
        }
        static void Layout(Transform parent, int padding, int spacing)
        {
            var l = parent.gameObject.AddComponent<VerticalLayoutGroup>(); l.padding = new RectOffset(padding, padding, padding, padding); l.spacing = spacing; l.childControlHeight = true; l.childControlWidth = true; l.childForceExpandHeight = false; l.childForceExpandWidth = true;
        }
        static void Rule(Transform parent)
        {
            var r = UiKit.Panel("Divider", parent, line); var layout = r.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = layout.minHeight = 1;
        }
        static Button RowButton(Transform parent, string name, string text, float h = 42)
        {
            var b = UiKit.Button(name, parent, text, font, button, ink); var el = b.gameObject.AddComponent<LayoutElement>(); el.preferredHeight = el.minHeight = h; return b;
        }
        static void BuildUI(AnatomyApp app)
        {
            var canvasGO = new GameObject("AnatoLab Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var root = canvasGO.transform;
            var header = UiKit.Panel("Header", root, panel); UiKit.Box(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1440, 88)); header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.sizeDelta = new Vector2(0, 88);
            var brand = UiKit.Text("Brand", header, "ANATO<color=#30DFBD>LAB</color>", font, 29, ink); UiKit.Box(brand.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 3), new Vector2(260, 44)); brand.fontStyle = FontStyles.Bold; brand.characterSpacing = 2;
            var headline = UiKit.Text("Subject", header, "HUMAN MUSCULAR SYSTEM", font, 14, ink); UiKit.Box(headline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(450, 25)); headline.alignment = TextAlignmentOptions.Center; headline.characterSpacing = 2;
            var sub = UiKit.Text("Subtitle", header, "ATLAS INTERAKTIF  /  KOMPUTER GRAFIS", font, 10, muted); UiKit.Box(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(450, 20)); sub.alignment = TextAlignmentOptions.Center; sub.characterSpacing = 2;
            var status = UiKit.Text("Status", header, "3D EXPLORER  <color=#30DFBD> /  LIVE</color>", font, 12, muted); UiKit.Box(status.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(230, 26)); status.alignment = TextAlignmentOptions.Right;
            var navigation = UiKit.Panel("Navigation", root, panel); navigation.anchorMin = new Vector2(0, 0); navigation.anchorMax = new Vector2(0, 1); navigation.pivot = new Vector2(0, 1); navigation.offsetMin = new Vector2(24, 120); navigation.offsetMax = new Vector2(308, -110);
            var navTitle = UiKit.Text("Title", navigation, "JELAJAHI ANATOMI", font, 13, accent); UiKit.Box(navTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -20), new Vector2(248, 25)); navTitle.characterSpacing = 2;
            var input = UiKit.Panel("Search", navigation, new Color(0.075f, 0.115f, 0.16f)); UiKit.Box(input, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -58), new Vector2(252, 46));
            app.search = input.gameObject.AddComponent<TMP_InputField>();
            var textArea = UiKit.Rect("Text Area", input); UiKit.Stretch(textArea, 12, 8, 40, 8); textArea.gameObject.AddComponent<RectMask2D>(); app.search.textViewport = textArea;
            var inputText = UiKit.Text("Text", textArea, "", font, 16, ink); UiKit.Stretch(inputText.rectTransform); app.search.textComponent = inputText; app.search.fontAsset = font; app.search.pointSize = 16; app.search.lineType = TMP_InputField.LineType.SingleLine;
            var placeholder = UiKit.Text("Placeholder", textArea, "Cari otot atau wilayah...", font, 14, muted); UiKit.Stretch(placeholder.rectTransform); app.search.placeholder = placeholder;
            app.clearSearchButton = UiKit.Button("Clear search", input, "X", font, Color.clear, muted, 12); UiKit.Box(app.clearSearchButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(32, 36));
            app.countText = UiKit.Text("Count", navigation, "MEMUAT WILAYAH...", font, 10, muted); UiKit.Box(app.countText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -118), new Vector2(248, 20)); app.countText.characterSpacing = 1;
            var scroll = UiKit.Panel("Muscle Scroll View", navigation, Color.clear); scroll.anchorMin = Vector2.zero; scroll.anchorMax = Vector2.one; scroll.offsetMin = new Vector2(14, 48); scroll.offsetMax = new Vector2(-14, -145);
            app.listScroll = scroll.gameObject.AddComponent<ScrollRect>(); app.listScroll.horizontal = false; app.listScroll.movementType = ScrollRect.MovementType.Clamped; app.listScroll.scrollSensitivity = 26;
            var viewport = UiKit.Panel("Viewport", scroll, Color.clear); UiKit.Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>(); app.listScroll.viewport = viewport;
            var content = UiKit.Rect("Content", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            Layout(content, 0, 6); var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; app.listContent = content; app.listScroll.content = content;
            var note = UiKit.Text("List note", navigation, "Gulir daftar untuk melihat seluruh wilayah", font, 10, muted); UiKit.Box(note.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(248, 25));
            var info = UiKit.Panel("InfoPanel", root, panel); info.anchorMin = new Vector2(1, 0); info.anchorMax = Vector2.one; info.pivot = new Vector2(1, 1); info.offsetMin = new Vector2(-358, 120); info.offsetMax = new Vector2(-24, -110); app.infoPanel = info.gameObject;
            Layout(info, 22, 12);
            app.regionText = Label(info, "Region", "WILAYAH OTOT", 11, accent, 22); app.regionText.characterSpacing = 2;
            app.titleText = Label(info, "TitleText", "Pilih wilayah otot", 27, ink, 76, true);
            app.latinText = Label(info, "Latin", "Nama anatomi", 15, muted, 44); app.latinText.fontStyle = FontStyles.Italic;
            Rule(info);
            var descScroll = UiKit.Panel("Description Scroll View", info, Color.clear); var flex = descScroll.gameObject.AddComponent<LayoutElement>(); flex.flexibleHeight = 1; flex.minHeight = 110;
            var descSR = descScroll.gameObject.AddComponent<ScrollRect>(); descSR.horizontal = false; descSR.movementType = ScrollRect.MovementType.Clamped; descSR.scrollSensitivity = 25;
            var descVP = UiKit.Panel("Viewport", descScroll, Color.clear); UiKit.Stretch(descVP); descVP.gameObject.AddComponent<RectMask2D>(); descSR.viewport = descVP;
            var descContent = UiKit.Rect("Content", descVP); descContent.anchorMin = new Vector2(0, 1); descContent.anchorMax = Vector2.one; descContent.pivot = new Vector2(0.5f, 1); descContent.anchoredPosition = Vector2.zero; descContent.sizeDelta = Vector2.zero;
            Layout(descContent, 0, 10); var descFit = descContent.gameObject.AddComponent<ContentSizeFitter>(); descFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; descSR.content = descContent;
            var desc = UiKit.Text("DescriptionText", descContent, "Deskripsi wilayah", font, 16, ink); desc.lineSpacing = 6; app.descriptionText = desc;
            Label(descContent, "Function heading", "FUNGSI UTAMA", 11, accent, 24, true);
            var fun = UiKit.Text("FunctionText", descContent, "Fungsi otot", font, 16, ink); fun.lineSpacing = 6; app.functionText = fun;
            Label(descContent, "Approximation", "CATATAN MODEL\nBatas pilihan adalah perkiraan wilayah permukaan dari satu mesh gabungan. Struktur dalam belum terpisah.", 12, muted, 100);
            app.focusButton = RowButton(info, "Focus", "FOKUS KE WILAYAH   [ F ]");
            app.closeButton = RowButton(info, "CloseButton", "TUTUP PILIHAN   [ ESC ]", 38);
            var welcome = UiKit.Panel("Welcome", root, panel); welcome.anchorMin = info.anchorMin; welcome.anchorMax = info.anchorMax; welcome.pivot = info.pivot; welcome.offsetMin = info.offsetMin; welcome.offsetMax = info.offsetMax; app.welcomePanel = welcome.gameObject;
            Layout(welcome, 24, 16);
            Label(welcome, "Kicker", "MULAI EKSPLORASI", 11, accent, 22);
            Label(welcome, "Headline", "Kenali tubuh.\nPahami geraknya.", 31, ink, 118, true);
            Label(welcome, "Intro", "Pilih wilayah pada model atau gunakan daftar di sebelah kiri untuk mempelajari otot dan fungsinya.", 17, muted, 115);
            Rule(welcome);
            Label(welcome, "One", "01   ARAHKAN KURSOR\nLihat highlight pada wilayah model.", 15, ink, 62);
            Label(welcome, "Two", "02   PILIH & PELAJARI\nKlik untuk membuka informasi.", 15, ink, 62);
            Label(welcome, "Three", "03   AMATI DARI SEGALA SISI\nPutar, zoom, dan gunakan X-ray.", 15, ink, 62);
            var spacer = UiKit.Rect("Spacer", welcome).gameObject.AddComponent<LayoutElement>(); spacer.flexibleHeight = 1;
            Label(welcome, "Disclaimer", "Model Meshy / wilayah permukaan perkiraan.\nUntuk pembelajaran visual dasar.", 11, muted, 48);
            info.gameObject.SetActive(false);
            var views = UiKit.Panel("View controls", root, panel); UiKit.Box(views, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-25, 122), new Vector2(390, 43));
            var buttons = new[] { app.frontButton = UiKit.Button("Front", views, "DEPAN", font, button, ink, 11), app.backButton = UiKit.Button("Back", views, "BELAKANG", font, button, ink, 11), app.leftButton = UiKit.Button("Left", views, "KIRI", font, button, ink, 11), app.rightButton = UiKit.Button("Right", views, "KANAN", font, button, ink, 11) };
            for (int i = 0; i < buttons.Length; i++) UiKit.Box(buttons[i].GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(4 + i * 96, 4), new Vector2(92, 35));
            var zooms = UiKit.Panel("Zoom controls", root, panel); UiKit.Box(zooms, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-376, 0), new Vector2(38, 84));
            app.zoomInButton = UiKit.Button("Zoom In", zooms, "+", font, button, ink, 22); UiKit.Box(app.zoomInButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(32, 36));
            app.zoomOutButton = UiKit.Button("Zoom Out", zooms, "-", font, button, ink, 22); UiKit.Box(app.zoomOutButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(32, 36));
            var footer = UiKit.Panel("Footer", root, panel); footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0); footer.pivot = Vector2.zero; footer.anchoredPosition = Vector2.zero; footer.sizeDelta = new Vector2(0, 98);
            app.selectionText = UiKit.Text("Selection status", footer, "MODE EKSPLORASI  /  SELURUH TUBUH", font, 11, accent); UiKit.Box(app.selectionText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -15), new Vector2(850, 24)); app.selectionText.characterSpacing = 1;
            app.hoverText = UiKit.Text("Hover", footer, "Arahkan kursor ke model untuk mengenali wilayah", font, 13, ink); UiKit.Box(app.hoverText.rectTransform, new Vector2(0, 0), Vector2.zero, new Vector2(28, 42), new Vector2(790, 25));
            var controls = UiKit.Text("Controls", footer, "PUTAR  Klik kanan + geser       ZOOM  Scroll       PILIH  Klik kiri       RESET  Home", font, 11, muted); UiKit.Box(controls.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28, 13), new Vector2(1000, 22));
            app.resetButton = UiKit.Button("Reset", footer, "RESET VIEW", font, button, ink, 12); UiKit.Box(app.resetButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26, 5), new Vector2(138, 40));
            app.xrayButton = UiKit.Button("XRay", footer, "X-RAY  ON", font, new Color(0.08f, 0.29f, 0.28f), accent, 12); UiKit.Box(app.xrayButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-176, 5), new Vector2(138, 40)); app.xrayText = app.xrayButton.GetComponentInChildren<TMP_Text>();
        }

        [MenuItem("AnatoLab/Periksa Scene", false, 1)]
        public static void Validate()
        {
            var organs = UnityEngine.Object.FindObjectsByType<OrganData>(FindObjectsSortMode.None);
            int colliders = organs.Sum(x => x.GetComponentsInChildren<MeshCollider>().Count(c => c.convex));
            var app = UnityEngine.Object.FindFirstObjectByType<AnatomyApp>();
            bool ok = organs.Length > 10 && colliders >= organs.Length && app != null && app.search != null && app.selector != null && app.closeButton != null && app.orbit != null;
            Debug.Log("ANATOLAB VALIDATION: " + (ok ? "PASS" : "FAIL") + " | OrganData=" + organs.Length + " ConvexColliders=" + colliders + " App=" + (app != null));
            EditorUtility.DisplayDialog("Pemeriksaan AnatoLab", (ok ? "PASS" : "FAIL") + "\nOrganData: " + organs.Length + "\nMeshCollider Convex: " + colliders + "\n\nLanjutkan pengujian interaksi di Play Mode.", "OK");
        }
    }
}
#endif

// AnatoLab package revision: 1795099051092822300
