using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using TMPro;
using static UnityEngine.ParticleSystem;
using Slider = UnityEngine.UIElements.Slider;
using UnityEngine.XR;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Drawing;
using Color = UnityEngine.Color;

[System.Serializable]
public class Reaction
{
    public string category;
    public string reactionName;
    public string filename;
    public int transitionState;
    public string description;
}


[System.Serializable]
public class ReactionsList
{
    public List<Reaction> reactions;
}

[System.Serializable]
public class Frame
{
    public List<string> lines = new List<string>();
}


public class MainScreen : MonoBehaviour

{
    [SerializeField]
    protected VisualTreeAsset uiDocument;
    private VisualElement rootElement;
    private VisualElement uiPage;
    private VisualElement reactionPage;
    private ScrollView container;
    private ReactionsList reactionsList;
    private Dictionary<string, List<Reaction>> categories = new Dictionary<string, List<Reaction>>();
    private float scrollVelocity;
    private bool isInertiaActive;
    private float decelerationRate = 2f;
    private float lowSpeedThreshold = 0.5f;
    List<Foldout> allFoldouts = new List<Foldout>();
    private bool _isExpanded;
    private Toggle toggleButton;
    private Button backButton;
    private Button backButton2;
    private Toggle playPauseToggle;
    private Button fastForwardButton;
    private Button fastBackwardButton;
    public Sprite playIcon;
    public Sprite pauseIcon;
    private Button TSButton;
    private Button descriptionButton;
    private Button speedButton;
    private int speedIndex;
    private Boolean beginingScaleFactor = true;
 


    //Reaction stuff
    private GameObject curReaction;
    private GameObject curBond;
    private GameObject wholeReaction;
    public Dictionary<string, Transform> Children = new Dictionary<string, Transform>();
    private Dictionary<int, String> AtomNames = new Dictionary<int, String>();
    private Dictionary<string, Color> originalColors = new Dictionary<string, Color>();

    private String pattern = @"^(\s+-?\d+\.?\d+){3}\s+\w{1,2}";
    private String bondpattern = @"^\s*\d+\s+\d+\s+(\d+|[0-9]*\.[0-9]+)(\s+\d+)*\s*$";
    private String endPattern = @"M  END";
    private String namePattern = @"([a-zA-Z]+)(\d+)";

    private float speed = 2f;

    public GameObject doubleBond;
    public GameObject singleBond;
    public GameObject tripleBond;
    public GameObject oneAndHalfBond;
    public GameObject halfBond;
    public GameObject twoAndHalfBond;
    public GameObject Line;
    public GameObject LineInstance;
    Vector3 fixedPostion = new Vector3(0, 2, 0);
    private int currentPanelIndex = 0;
    private int currentIndex = 0;
    private int frameIndex = 0;

    private List<String> hightLighted;
    private List<String> readText;
    private int totalFrames = 1;
    private bool isRead = false;

    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private bool isPlaying = true;
    private bool isUI = true;


    private RectTransform content;
    private int numberOfButtons = 200;

    private string folderPath = "sdf";



    private UnityEngine.UIElements.Label elements_angle;
    private UnityEngine.UIElements.Label reaction_name;
    private UnityEngine.UIElements.Label description;


    private List<GameObject> cylinders = new List<GameObject>();


    private int namecounter = 0;


    private float distanceFromTarget = 10.0f;
    public List<String> panelsInfo;

    private Touch initTouch = new Touch();
    public Camera cam;
    private float rotX = 0f;
    private float rotY = 0f;
    private Vector3 origRot;
    public float rotSpeed = 0.3f;
    public float dir = -1;
    private float MinX, MaxX, MinY, MaxY;

    private float timer = 0f;
    private float interval = 1f; // Interval in seconds

    private float zoomMin = 0.1f;
    private float zoomMax = 179.0f;

    private Slider frameSlider;
    private VisualElement bottom;
    private VisualElement top;
    private VisualElement viewWindow;
    private VisualElement safeAreaOffset;

    private bool isLoading = false;
    // Flag to indicate if the slider is being updated programmatically
    private bool isUpdatingFromCode = false;

    private EventCallback<ClickEvent> currentCallback;
    private EventCallback<ClickEvent> TSCallback;

    public bool IsExpanded
    {
        get { return _isExpanded; }
        set
        {
            _isExpanded = value;
            toggleButton.value = value;
            // Perform the logic to expand or collapse based on the new value
            // Update the Toggle's value here if needed
            // For example: toggle.value = value;
        }
    }


    private void CreateCategories()
    {
       
        string path = Path.Combine(UnityEngine.Application.streamingAssetsPath, "reactions.json");
        var loadingRequest = UnityWebRequest.Get(path);
        loadingRequest.SendWebRequest();
        while (!loadingRequest.isDone && !loadingRequest.isNetworkError && !loadingRequest.isHttpError) ;
        string jsonString = System.Text.Encoding.UTF8.GetString(loadingRequest.downloadHandler.data);

        //string jsonFilePath = Path.Combine(Application.dataPath, "reactions.json");
        //string jsonString = File.ReadAllText(jsonFilePath);
        reactionsList = JsonUtility.FromJson<ReactionsList>(jsonString);

        foreach (Reaction reaction in reactionsList.reactions)
        {               
            if (categories.ContainsKey(reaction.category))
            {
                categories[reaction.category].Add(reaction);
            }
            else
            {
                List<Reaction> newList = new List<Reaction> { reaction };
                categories.Add(reaction.category, newList);
            }
        }

        foreach (string category in categories.Keys)
        {
            var foldout = new Foldout { text = category, value = false };
            foldout.AddToClassList("my-foldout");
            container.Add(foldout);
            VisualElement contentContainer = new VisualElement();
            //contentContainer.AddToClassList("my-foldout-content");

            // Add your content to the container
            // For example: contentContainer.Add(new Label("Content here"));

            // Add the container to the foldout

            // Add two buttons to each foldout 
            foreach (Reaction reaction in categories[category])
            {
                var button = new Button(() => OnClickReactionButton(reaction))
                {
                    text = reaction.reactionName
                };
                button.AddToClassList("my-button");
                contentContainer.Add(button);
            }
            foldout.Add(contentContainer);
            foldout.RegisterValueChangedCallback(evt =>
            {
                IsExpanded = allFoldouts.Any(f => f.value);

            });
            allFoldouts.Add(foldout);

        }
        toggleButton = rootElement.Q<Toggle>("expandall");
        toggleButton.AddToClassList("my-toggle-button");
        toggleButton.value = false;
        toggleButton.label = toggleButton.value ? "\u2191 Collapse All" : "\u2193 Expand All";
        //toggleButton.RegisterValueChangedCallback(evt =>
        //{
        //    toggleButton.label = evt.newValue ? "Collapse All" : "Expand All";
        //});
        toggleButton.RegisterCallback<ClickEvent>(evt =>
        {
            toggleButton.label = toggleButton.value ? "\u2191 Collapse All" : "\u2193 Expand All";
            if (toggleButton.value)
            {
                ExpandAllFoldouts();
            }
            else
            {
                CollapseAllFoldouts();
            }
        });


    }

    void OnEnable()
    {
        // Ensure the uiDocument is assigned
        if (uiDocument == null)
        {
            Debug.LogError("UI Document is not assigned in the Inspector.");
            return;
        }

        var uiDocumentComponent = GetComponent<UIDocument>();
        rootElement = uiDocument.CloneTree();
        if (uiDocumentComponent != null)
        {
            uiDocumentComponent.rootVisualElement.Clear();
            uiDocumentComponent.rootVisualElement.Add(rootElement);
        }
        else
        {
            Debug.LogError("No UIDocument component found.");
            return;
        }
        uiPage = rootElement.Q<VisualElement>("ui_page_1");
        reactionPage = rootElement.Q<VisualElement>("ui_page_2");
        elements_angle = rootElement.Q<UnityEngine.UIElements.Label>("elements_angle");
        reaction_name = rootElement.Q<UnityEngine.UIElements.Label>("reaction_name");
        description = rootElement.Q<UnityEngine.UIElements.Label>("description");
        descriptionButton = rootElement.Q<Button>("Description_Button");
        playPauseToggle = rootElement.Q<Toggle>("pauseButton");
        bottom = rootElement.Q<VisualElement>("Bottom");
        top = rootElement.Q<VisualElement>("Top");
        viewWindow = rootElement.Q<VisualElement>("ViewWindow");
        safeAreaOffset = rootElement.Q<VisualElement>("SafeAreaOffset");
        safeAreaOffset.style.height = Screen.height - Screen.safeArea.yMax;


        ColorAllButtons();
        UpdateIcon(playPauseToggle.value);
        playPauseToggle.RegisterValueChangedCallback(evt =>
        {
            Pause();
            UpdateIcon(isPlaying);
        });
       
        TSButton = rootElement.Q<Button>("TSButton");

        speedButton = rootElement.Q<Button>("SpeedButton");
        speedButton.text = "1X";  // Set initial button text to "1X"

        // Current speed settings
        List<string> speeds = new List<string> { "0.5X", "1X", "2X", "3X", "4X", "5X", "10X" };
        speedIndex = 1;  // Start at "1X", which corresponds to speeds[1]

        speedButton.clicked += () =>
        {
            // Move to the next item in the list cyclically
            speedIndex = (speedIndex + 1) % speeds.Count;
            speedButton.text = speeds[speedIndex];

            // Update the playback rate based on selected speed
            switch (speeds[speedIndex])
            {
                case "0.5X":
                    playbackRate = 2f;
                    break;
                case "1X":
                    playbackRate = 5f;
                    break;
                case "2X":
                    playbackRate = 10f;
                    break;
                case "3X":
                    playbackRate = 15f;
                    break;
                case "4X":
                    playbackRate = 20f;
                    break;
                case "5X":
                    playbackRate = 25f;
                    break;
                case "10X":
                    playbackRate = 50f;
                    break;
                default:
                    playbackRate = 5f;  // Default to 1X speed if none of the options match
                    break;
            }

            Debug.Log($"Updated Speed: {playbackRate}");
        };

        container = rootElement.Q<ScrollView>("container");
        isInertiaActive = false;

        container.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
        container.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

        if (container == null)
        {
            Debug.LogError("Container is null. Check the UXML file for a ScrollView with the correct identifier.");
            return;
        }

        container = rootElement.Q<ScrollView>("container");

        CreateCategories();
        currentPanelIndex = 0;
    }

    //New Frame Approach
    private List<Frame> frames = new List<Frame>();

    // This method starts the coroutine and assumes 'filePath' is a valid path to your file
    public void LoadFrameData(string filePath)
    {
        StartCoroutine(LoadFileFromStreamingAssets(filePath));

    }

    IEnumerator LoadFileFromStreamingAssets(string filePath)
    {
        isLoading = true;

        Debug.Log("File before unitywebrequest: " + filePath);

        using (UnityWebRequest loadingRequest = UnityWebRequest.Get(filePath))
        {
            yield return loadingRequest.SendWebRequest();

            if (loadingRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Loading error: " + loadingRequest.error);
                yield break;
            }

            Debug.Log("File found: " + filePath);
            string textString = System.Text.Encoding.UTF8.GetString(loadingRequest.downloadHandler.data);

            List<string> readLines = new List<string>(textString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
            Frame currentFrame = new Frame();

            foreach (var line in readLines)
            {
                if (line.Trim() == "M  END")
                {
                    currentFrame.lines.Add(line);
                    frames.Add(currentFrame);
                    currentFrame = new Frame(); // Prepare for a new frame
                }
                else
                {
                    currentFrame.lines.Add(line); // Add line to the current frame
                }
            }

            // Process frames as needed

            Debug.Log($"Finished processing. Total frames: {frames.Count}");
            totalFrames = frames.Count;
            foreach (Transform cld in curReaction.transform)
            {

                // Debug.Log(cld.name);
                Children.Add(cld.name, cld);
                cld.gameObject.AddComponent<BoxCollider>();
                Match match = Regex.Match(cld.name, namePattern);
                AtomNames.Add(int.Parse(match.Groups[2].Value), cld.name);

            }

        }


        isLoading = false;
    }


    private void OnClickReactionButton(Reaction reaction)
    {
        Debug.Log($"Clicked Button {reaction.filename}");


        currentPanelIndex = 2;
        //turn off the uiPage and turn on the reactionPage
        uiPage.visible = false;
        reactionPage.visible = true;
        //load the reaction and set up variables
        Children = new Dictionary<string, Transform>();
        AtomNames = new Dictionary<int, String>();
        currentIndex = 0;
        frameIndex = 0;
        totalFrames = 1;
        playbackRate = 5f;
        if(speedButton != null)
            speedButton.text = "1X";
        speedIndex = 1;
        isPlaying = true;
        beginingScaleFactor = true;
        MinX = 0;
        MinY = 0;
        MaxX = 0;
        MaxY = 0;



        UpdateIcon(isPlaying);

        reaction_name.text = "<align=\"justified\"><font-weight=100><size=42px>" + reaction.reactionName + "</size></font-weight></align>";

        //reaction_name.visible = true;
        wholeReaction = new GameObject();
        wholeReaction.name = "wholeReaction";
        //set the whole reaction's position to the fixed position
        wholeReaction.transform.position = fixedPostion;
        curReaction = InstantiatePrefabByName(reaction.filename, wholeReaction);
        curReaction.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
        curBond = new GameObject();
        curBond.name = "Bonds";
        curBond.transform.SetParent(wholeReaction.transform);

        if(TSButton != null)
        {
            if(reaction.transitionState > 0)
            {
                //change the color of the text in the button to be blue
                

                TSButton.style.color = Color.blue;
                TSButton.SetEnabled(true);
                TSCallback = evt => JumpToTS(evt, reaction.transitionState);
                TSButton.RegisterCallback(TSCallback);
            }
            else
            {
                TSButton.style.color = Color.gray;
                TSButton.SetEnabled(false);
            }


        }

        if(descriptionButton != null)
        {
            Debug.Log("Description Button is not null");
            if (!string.IsNullOrEmpty(reaction.description))
            {
                Debug.Log("Description is enabled" + reaction.description);
                description.visible = false;
                descriptionButton.SetEnabled(true);
                descriptionButton.style.color = Color.blue;
                descriptionButton.text = "\u2191 Show Rxn Info";
                // Create a new callback and register it
                currentCallback = evt => OnDescriptionButtonClick(evt, reaction);
                descriptionButton.RegisterCallback(currentCallback);
            }
            else
            {
                Debug.Log("Description is disabled");
                descriptionButton.SetEnabled(false);
                descriptionButton.style.color = Color.gray;
                descriptionButton.text = "\u2191 Show Rxn Info";
            }
            
        }

        hightLighted = new List<string>();
        LineInstance = Instantiate(Line, Vector3.zero, Quaternion.identity);
        LineInstance.transform.SetParent(wholeReaction.transform, false);
        LineRenderer lineRenderer = LineInstance.GetComponent<LineRenderer>();

        // Set properties for the LineRenderer
        lineRenderer.positionCount = 1;

        string filePath = Path.Combine(Application.streamingAssetsPath, reaction.filename);
        //filePath = filePath.Replace("/", "\\");
        filePath = filePath + ".sdf";

        Debug.Log("File before found: " + filePath);

        readText = new List<string>();
        LoadFrameData(filePath);

        isRead = true;
        //}

        isUI = false;
    }

    GameObject InstantiatePrefabByName(string prefabName, GameObject wholeReaction)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab != null)
        {

            GameObject instantiatedPrefab = Instantiate(prefab, Vector3.zero, transform.rotation, wholeReaction.transform);
            instantiatedPrefab.transform.localPosition = Vector3.zero;

            return instantiatedPrefab;
        }
        else
        {
            Debug.LogWarning("Prefab with name " + prefabName + " not found in Resources folder.");
            return null;
        }

    }



    //Start is called before the first frame update
    void Start()
    {



    }

    private void OnScrollWheel(WheelEvent evt)
    {
        float scrollDelta = evt.delta.y;
        StartInertia(scrollDelta);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        StopInertia();
    }

    void zoom(float increment)
    {
        cam.fieldOfView += increment;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, zoomMin, zoomMax);
        //cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - increment, zoomMin, zoomMax);
    }

    public void OnClickBackButton()
    {
        // Hide the current panel

        // If the index is less than 0, exit the application
        if (currentPanelIndex <= 0)
        {
            Application.Quit();
            return;
        }
        if (currentPanelIndex == 2)
        {
            //secondCanvas.gameObject.SetActive(!secondCanvas.gameObject.activeSelf);
            //parentCanvas.gameObject.SetActive(!parentCanvas.gameObject.activeSelf);
            //panelsInfo.RemoveAt(1);
            uiPage.visible = true;
            reactionPage.visible = false;
            Destroy(curReaction);
            Destroy(wholeReaction);
            Destroy(LineInstance);
            Children = new Dictionary<string, Transform>();
            AtomNames = new Dictionary<int, String>();
            frames = new List<Frame>();
            elements_angle.text = "";
            isUI = true;
            currentPanelIndex = 0;
            frameIndex = 0;
            isPlaying = false;
            description.text = "";
            description.visible = false;
            beginingScaleFactor = true;
            originalColors = new Dictionary<string, Color>();
            if (currentCallback != null)
            {
                descriptionButton.UnregisterCallback<ClickEvent>(currentCallback);
                currentCallback = null;
            }
            if (TSCallback != null)
            {
                TSButton.UnregisterCallback<ClickEvent>(TSCallback);
                TSCallback = null;
            }


            //TODO: show previous panel
            //oncategorybuttonclick(panelsinfo[0]);
        }
        else
        {
            //Destroy(subCategoryPanelInstance);
            //TODO: show previous panel

        }

    }

    private void OnDescriptionButtonClick(ClickEvent evt, Reaction reaction)
    {
        if (description.visible)
        {
            Debug.Log("Description is visible, change to invisible");
            description.text = "";
            description.visible = false;
            descriptionButton.text = "\u2191 Show Rxn Info";
        }
        else
        {
            Debug.Log("Description is invisible, change to visible");
            description.text = "<align=\"justified\"><font-weight=100><size=25px>" + reaction.description + "</size></font-weight></align>";
            description.visible = true;
            descriptionButton.text = "\u2193 Hide Rxn Info";
        }
    }


    private float scaleFactor;
    void GetClickedObject(Vector2 pos)
    {
        Vector2 touchPos = Input.GetTouch(0).position; 
        Debug.Log("GetClickedObject : " + touchPos);
        var ray = cam.ScreenPointToRay(pos);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2.0f);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Hit Object: " + hit.collider.gameObject.name);
            foreach (KeyValuePair<string, Transform> entry in Children)
            {
                if (hit.transform == entry.Value)
                {
                    Debug.Log(entry.Key + "Clikced");
                    Renderer objRenderer = entry.Value.GetComponent<Renderer>();
                    Material mat = objRenderer.material; // Get a reference to the material
                    if (hightLighted.Contains(entry.Key))
                    {
                        //Color highlightedColor = mat.GetColor("_Color");
                        //Color originalColor = highlightedColor / 2.0f;

                        // Revert to the original color
                        Color originalColor = originalColors[entry.Key];
                        mat.SetColor("_Color", originalColor);

                        // Remove the entry from the highlighted list and originalColors dictionary
                        hightLighted.Remove(entry.Key);
                        originalColors.Remove(entry.Key);




                    }
                    else if (hightLighted.Count < 4)
                    {
                        Color originalColor = mat.GetColor("_Color");
                        if (!originalColors.ContainsKey(entry.Key))
                        {
                            originalColors.Add(entry.Key, originalColor);
                        }
                        //Color highlightedColor = originalColor * 2.0f; // Increase brightness

                        Color highlightColor = Color.gray; // Define the highlight color, e.g., yellow
                        Color highlightedColor = Color.Lerp(originalColor, highlightColor, 0.5f);
                        highlightedColor = Color.Lerp(Color.red, highlightedColor, 0.95f) * 1.5f;
                       
                        mat.SetColor("_Color", highlightedColor); // Set the material color
                        //mat.EnableKeyword("_EMISSION"); // Enable emission keyword
                        //mat.SetColor("_EmissionColor", originalColor); // Set the emission color
                        hightLighted.Add(entry.Key);

                    }

                }
            }

            //TODO: determine if this code should be deleted since the target is not being used
            //if (!isPointerOverUIObject()) { target = hit.collider.gameObject; }
        }



    }

    private bool isPointerOverUIObject()
    {
        Debug.Log("Checking if pointer is over a UI object");
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current is null");
            return false;
        }
        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = Input.GetTouch(0).position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        //return results.Count > 0;
        if (results.Count > 0)
        {
            //TODO: the topmost UI element is always the panelsettings of uitoolkit
            // Log the name of the topmost UI element hit by the raycast
            Debug.Log("Topmost UI element clicked: " + results[0].gameObject.name);
            var panelHandler = results[0].gameObject.GetComponent<PanelEventHandler>();
            if (panelHandler != null)
            {
                Vector2 localPoint;
                IPanel panel = panelHandler.panel;
                localPoint = RuntimePanelUtils.ScreenToPanel(panel, Input.mousePosition);
                if (localPoint != null)
                {
                    Debug.Log("Local Point: " + localPoint + "Bottom : " + bottom.worldBound);
                    Vector2 correctedPoint = new Vector2(localPoint.x, localPoint.y + bottom.worldBound.y);
                    if (bottom.worldBound.Contains(correctedPoint))
                    {
                        Debug.Log("Bottom Clicked +: " + correctedPoint);
                        return false;
                    }
                    VisualElement element = panel.Pick(localPoint);
                    if (element != null)
                    {
                        Debug.Log("UI Toolkit Element under pointer: " + element.name);
                    }
                }
            }
            return true;
        }
        return false;
    }

    private bool IsTouchInsideViewWindow()
    {
       
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current is null");
            return false;
        }

        if (Input.touchCount == 0)  // Efficiently handle no-touch scenarios
        {
            return false;
        }

        PointerEventData ped = new PointerEventData(EventSystem.current);
        List<RaycastResult> results = new List<RaycastResult>();

        for (int i = 0; i < Input.touchCount; i++)  // Loop through all available touches
        {
            Touch touch = Input.GetTouch(i);
            ped.position = touch.position;
            EventSystem.current.RaycastAll(ped, results);

            foreach (RaycastResult result in results)
            {
                var panelHandler = result.gameObject.GetComponent<PanelEventHandler>();
                if (panelHandler != null)
                {
                    IPanel panel = panelHandler.panel;
                    Vector2 localPoint = RuntimePanelUtils.ScreenToPanel(panel, touch.position);

                    if (localPoint != null)
                    {
                        Vector2 correctedPoint = new Vector2(localPoint.x, rootElement.worldBound.height - localPoint.y);
                        if (viewWindow.worldBound.Contains(correctedPoint))
                        {
                            return true;
                        }
                    }
                    
                }
            }
        }

        return false;
    }

    public static float CalcDihedral(Vector3 posA, Vector3 posB, Vector3 posC, Vector3 posD)
    {
        Vector3 B1 = posB - posA;
        Vector3 B2 = posC - posB;
        Vector3 B3 = posD - posC;

        float modB2 = B2.magnitude;

        // yA is the result of modulus of B2 times B1
        Vector3 yA = modB2 * B1;

        // CP2 is the cross product of B2 and B3
        Vector3 CP2 = Vector3.Cross(B2, B3);
        float termY = Vector3.Dot(yA, CP2);

        // CP is the cross product of B1 and B2
        Vector3 CP = Vector3.Cross(B1, B2);
        float termX = Vector3.Dot(CP, CP2);

        float dihed4 = Mathf.Atan2(termY, termX) * 180 / Mathf.PI;
        return dihed4;
    }

    private void drawHighlightedLines()
    {         //generate hightlighted lines
        if (hightLighted.Count > 0)
        {
            //ShowElementAngleText();
            // Format each string and combine them
            string combinedString = "";
            foreach (string str in hightLighted)
            {
                string text = Regex.Replace(str, @"\d+", "");
                combinedString += $"Atom:       {text}\n";
            }
            elements_angle.text = combinedString;
            // Get the LineRenderer component from the instantiated object
            LineRenderer lineRenderer = LineInstance.GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;

            // Set properties for the LineRenderer
            lineRenderer.positionCount = hightLighted.Count; // We're drawing a simple line, so we need 2 positions

            // Set the positions for the LineRenderer
            for (int i = 0; i < hightLighted.Count; i++)
            {
                Vector3 localPosition = wholeReaction.transform.InverseTransformPoint(Children[hightLighted[i]].position);
                lineRenderer.SetPosition(i, localPosition);
                //lineRenderer.SetPosition(i, Children[hightLighted[i]].position);
                //lineRenderer.transform.SetParent(curBond.transform);
            }

            if (hightLighted.Count == 2)
            {
                float distance = (float)Math.Round(Vector3.Distance(Children[hightLighted[0]].localPosition, Children[hightLighted[1]].localPosition), 2);
                combinedString += $"Distance:       {distance}\n";
                elements_angle.text = combinedString;
            }
            if (hightLighted.Count == 3)
            {
                Vector3 side1 = Children[hightLighted[0]].position - Children[hightLighted[1]].position;
                Vector3 side2 = Children[hightLighted[2]].position - Children[hightLighted[1]].position;
                combinedString += $"Angle:       {(int)Math.Round(Vector3.Angle(side1, side2))}\n";
                elements_angle.text = combinedString;
            }
            if (hightLighted.Count == 4)
            {
                float dia_angle = CalcDihedral(Children[hightLighted[0]].position, Children[hightLighted[1]].position, Children[hightLighted[2]].position, Children[hightLighted[3]].position);
                combinedString += $"Diahedral Angle:       {(int)Math.Round(dia_angle)}\n";
                elements_angle.text = combinedString;
            }

        }
        else
        {
            elements_angle.text = "";
        }
    }

    private void SetButtonText(GameObject button, string text)
    {
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        buttonText.text = text;
    }


    private float playbackRate = 5f; // Frames per second
    private float timeSinceLastFrameChange = 0.0f;

    private void Update()
    {
        if (isInertiaActive)
        {
            Vector2 currentOffset = container.scrollOffset;
            currentOffset.y += scrollVelocity * Time.deltaTime;
            container.scrollOffset = currentOffset;

            scrollVelocity -= scrollVelocity * decelerationRate * Time.deltaTime;
            // New code: Reduce inertia more quickly at low speeds
            if (Mathf.Abs(scrollVelocity) < lowSpeedThreshold)
            {
                scrollVelocity -= scrollVelocity * decelerationRate * Time.deltaTime * 2; // Increase deceleration
            }
            else
            {
                scrollVelocity -= scrollVelocity * decelerationRate * Time.deltaTime;
            }


            if (Mathf.Abs(scrollVelocity) < 0.1f)
                StopInertia();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClickBackButton();
        }

        if (cam.fieldOfView < zoomMin)
        {
            cam.fieldOfView = zoomMin;
        }
        else if (cam.fieldOfView > zoomMax)
        {
            cam.fieldOfView = zoomMax;
        }


        if (isUI || isLoading){
            Debug.Log("UI is active");
            return;
        }




        if (Input.touchCount > 0 && IsTouchInsideViewWindow())
        {
            if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
                {
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrePos = touchOne.position - touchOne.deltaPosition;
                    float preMagitude = Vector2.Distance(touchZeroPrevPos, touchOnePrePos);
                    float currentMagnitude = Vector2.Distance(touchZero.position, touchOne.position);
                    float difference = preMagitude - currentMagnitude;

                    if (Mathf.Abs(currentMagnitude) > 600.0f)
                    {
                        zoom(difference * 0.1f);
                    }
                    else
                    {
                        Vector2 direction = (touchZeroPrevPos - touchZero.position);
                        cam.transform.position += new Vector3(direction.x, direction.y, 0.0f) * 0.10f / cam.fieldOfView;
                    }

                }



            }
            else if (Input.touchCount == 1)
            {
                foreach (Touch touch in Input.touches)
                {

                    if (touch.phase == TouchPhase.Began)
                    {
                        initTouch = touch;
                        GetClickedObject(touch.position);

                    }
                    else if (touch.phase == TouchPhase.Moved)
                    {
                        // Calculate rotation around the y-axis
                        float rotationY = touch.deltaPosition.x * rotSpeed;
                        // Calculate rotation around the x-axis
                        float rotationX = -touch.deltaPosition.y * rotSpeed;

                        //wholeReaction.transform.Rotate(touch.deltaPosition.y * rotSpeed, -touch.deltaPosition.x * rotSpeed, 0, Space.Self);
                        wholeReaction.transform.Rotate(Vector3.up, rotationY, Space.World);
                        wholeReaction.transform.Rotate(Vector3.right, rotationX, Space.World);

                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        initTouch = new Touch();
                    }
                }
            }
        }

        float frameInterval = 1.0f / playbackRate;

        // Accumulate elapsed time
        timeSinceLastFrameChange += Time.deltaTime;

        // Determine how many frames to advance based on the elapsed time and playback rate
        int framesToAdvance = Mathf.FloorToInt(timeSinceLastFrameChange / frameInterval);

        // Check if it's time to advance to the next frame based on the playback rate
        if (framesToAdvance > 0 && isPlaying)
        {
            // Reset the accumulator
            timeSinceLastFrameChange -= framesToAdvance * frameInterval; // Use subtraction to maintain accuracy over time

            // Advance the frame index, taking care to loop back to the start as needed
            frameIndex = (frameIndex + framesToAdvance) % frames.Count;

            // Render the current frame
            RenderFrame();
        }

        drawHighlightedLines();

    }

    bool IsOutOfScreen(Vector3 worldPosition)
    {
        // Convert world position to viewport position
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(worldPosition);

        // Check if the position is outside the viewport bounds
        return viewportPosition.x < 0 || viewportPosition.x > 1 ||
               viewportPosition.y < 0 || viewportPosition.y > 1;
    }

    private void RenderFrame() {
        List<String> atoms = new List<String>();
        List<String> atomNameIndex = new List<String>();
        List<String> bonds = new List<String>();
        namecounter = 1;
        currentIndex = 0;
        while (isRead && currentIndex < frames[frameIndex].lines.Count)
        {

            DestroyObjects();
            //string s = readText[currentIndex];
            //currentIndex = (currentIndex + 1) % readText.Count;
            string s = frames[frameIndex].lines[currentIndex];
            currentIndex = (currentIndex + 1);
            Match match = Regex.Match(s, pattern);
            if (match.Success)
            {

                // Debug.Log(match.Value.Substring(match.Value.Length - 1) + namecounter );
                atoms.Add(match.Value);
                atomNameIndex.Add(match.Value.Substring(match.Value.Length - 1));
                namecounter += 1;

            }

            match = Regex.Match(s, bondpattern);
            if (match.Success)
            {
                bonds.Add(match.Value);
            }


            match = Regex.Match(s, endPattern);
            if (match.Success)
            {
                for (int i = 1; i < namecounter; i++)
                {
                    String line = atoms[i - 1];
                    String[] atomPos = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Vector3 position = new Vector3(float.Parse(atomPos[0]), float.Parse(atomPos[1]), float.Parse(atomPos[2]));


                    if (beginingScaleFactor)
                    {

                        MinX = Math.Min(MinX, position.x);

                        MaxX = Math.Max(MaxX, position.x);

                        float reactionWidth = MaxX - MinX;
                        var width = Camera.main.orthographicSize * 2.0 * Screen.width / Screen.height;

                        scaleFactor = (float)width / reactionWidth;





                    }

                    String atomName = atomPos[3] + i.ToString().PadLeft(namecounter.ToString().Length, '0');
                    if (frameIndex == 0)
                    {
                        String atom = AtomNames[i];
                        Children[atom].localPosition = position;
                    }
                    else
                    {
                        String atom = AtomNames[i];
                        //Vector3 newPosition = Vector3.Lerp(Children[atom].localPosition, position, Time.deltaTime * speed);
                        Children[AtomNames[i]].localPosition = position;
                    }



                }
                if (beginingScaleFactor)
                {
                    if(scaleFactor > 0.36f)
                    {
                        //curReaction.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
                    }
                    else
                    {
                        curReaction.transform.localScale = new Vector3(scaleFactor * 0.5f, scaleFactor * 0.5f, scaleFactor * 0.5f);
                    }


                    Debug.Log("First scale frame: " + frameIndex + "Scale Factor: " + scaleFactor);
                    beginingScaleFactor = false;
                }

                foreach (String bond in bonds)
                {

                    String[] connection = bond.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);



                    String bondType = connection[2];
                    String atom1 = AtomNames[int.Parse(connection[0])];
                    String atom2 = AtomNames[int.Parse(connection[1])];
                    Vector3 midpoint = (Children[atom1].position + Children[atom2].position) / 2f;

                    switch (bondType)
                    {

                        case "0.5":
                            ExpandObjects(halfBond, Children[atom1].position, Children[atom2].position, "repeat");
                            break;
                        case "1":
                            try
                            {
                                ExpandObjects(singleBond, Children[atom1].position, Children[atom2].position, "stretch");

                            }
                            catch (NullReferenceException e)
                            {
                                Debug.LogError("Caught a null reference exception: " + e.Message);
                                // Handle the exception or recover from it
                            }

                            break;
                        case "1.5":
                            ExpandObjects(oneAndHalfBond, Children[atom1].position, Children[atom2].position, "stretch");
                            break;
                        case "2":
                            ExpandObjects(doubleBond, Children[atom1].position, Children[atom2].position, "stretch");
                            break;
                        case "2.5":
                            ExpandObjects(twoAndHalfBond, Children[atom1].position, Children[atom2].position, "repeat");
                            break;
                        case "3":
                            ExpandObjects(tripleBond, Children[atom1].position, Children[atom2].position, "stretch");
                            break;
                        default:
                            ExpandObjects(singleBond, Children[atom1].position, Children[atom2].position, "stretch");
                            break;
                    }

                }
                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    // Perform your calculation here
                    // float result = CalcDihedral(...);

                    timer = interval; // Reset the timer
                }


                namecounter = 1;

                frameIndex = (frameIndex + 1) % frames.Count;
                break;
            }


            // cylinders.Clear();


            //update the progress with frameIndex/totalFrames as a integer
            
        }
        UpdateProgressBar(Mathf.FloorToInt((float)frameIndex / (float)totalFrames * 100));
    }

    public void ExpandObjects(GameObject objectToExpand, Vector3 startPoint, Vector3 endPoint, string mode)
    {
        if (objectToExpand != null)
        {
            //Renderer rend = objectToExpand.GetComponent<Renderer>();
            //if (!rend)
            //{
            //    Debug.LogError("objectToExpand does not have a Renderer component!");
            //    return;
            //}
            //float objectHeight = rend.bounds.size.y ;

            float totalDistance = Vector3.Distance(startPoint, endPoint);

            if (mode.ToLower() == "stretch")
            {
                Vector3 newPosition = (startPoint + endPoint) / 2;
                GameObject newObj = Instantiate(objectToExpand, newPosition, Quaternion.identity, transform);
                Vector3 direction = (endPoint - startPoint).normalized;
                newObj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                newObj.transform.localScale = new Vector3(newObj.transform.localScale.x, totalDistance / 2, newObj.transform.localScale.z);
                instantiatedObjects.Add(newObj);
                newObj.transform.SetParent(curBond.transform);
            }
            else if (mode.ToLower() == "repeat")
            {
                // Repeat mode: Repeat the object along the distance
                //float repeatHeight = 0.33f;
                //int numberOfCopies = Mathf.CeilToInt(totalDistance / repeatHeight);

                //for (int i = 0; i < numberOfCopies; i++)
                //{
                //    float t = numberOfCopies > 1 ? (float)i / (numberOfCopies - 1) : 0;
                //    Vector3 newPosition = Vector3.Lerp(startPoint, endPoint, t);

                //    if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z))
                //    {
                //        Debug.LogError("Trying to instantiate with NaN position: " + t + newPosition + startPoint + endPoint);
                //        return;
                //    }

                //    GameObject newObj = Instantiate(objectToExpand, newPosition, Quaternion.identity, transform);
                //    Vector3 direction = (endPoint - startPoint).normalized;
                //    newObj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                //    newObj.transform.localScale = new Vector3(newObj.transform.localScale.x, repeatHeight, newObj.transform.localScale.z);
                //    instantiatedObjects.Add(newObj);
                //    newObj.transform.SetParent(curBond.transform);

                // 2. Adjust the number of copies and their positioning based on the actual height
                float cylinderHeight = objectToExpand.transform.localScale.y;
                int numberOfCopies = Mathf.FloorToInt(totalDistance / 0.25f);
                Vector3 direction = (endPoint - startPoint).normalized; // Adjusted direction
                Vector3 currentPosition = startPoint;
                for (int i = 0; i < numberOfCopies; i++)
                {

                    GameObject newObj = Instantiate(objectToExpand, currentPosition, Quaternion.identity, transform);

                    currentPosition += direction * 0.25f;
                    newObj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                    newObj.transform.localScale = new Vector3(newObj.transform.localScale.x, 0.5f, newObj.transform.localScale.z);
                    instantiatedObjects.Add(newObj);
                    newObj.transform.SetParent(curBond.transform);
                }
            }
            else
            {
                Debug.LogError("Invalid mode specified. Use 'stretch' or 'repeat'.");
            }
        }
    }

    public void DestroyObjects()
    {

        Destroy(curBond);
        curBond = new GameObject();
        curBond.name = "Bonds";
        curBond.transform.SetParent(wholeReaction.transform);
        instantiatedObjects.Clear();
    }

    void ColorAllButtons()
    {
        var fastForwardIcon = Resources.Load<Texture2D>("icons/fast-forward-512");
        var fastBackwardIcon = Resources.Load<Texture2D>("icons/rewind");
        var backButtonIcon = Resources.Load<Texture2D>("backward-96");

        fastForwardButton = rootElement.Q<Button>("fastforward");
        if (fastForwardButton != null)
        {
            fastForwardButton.style.backgroundImage = new StyleBackground(fastForwardIcon);
            // Programmatically change the tint color of the element
            // Note: This specific approach does not apply directly as UI Toolkit does not support backgroundImageTintColor in USS
            // You'd need to adjust the material or shader of the image asset used or manage color tinting through other means
            fastForwardButton.style.unityBackgroundImageTintColor = Color.blue; // RGBA
        }
        fastForwardButton.RegisterCallback<ClickEvent>(evt =>
        {
            FastForward();
        });

        fastBackwardButton = rootElement.Q<Button>("fastbackward");
        if (fastBackwardButton != null)
        {
            // Programmatically change the tint color of the element
            // Note: This specific approach does not apply directly as UI Toolkit does not support backgroundImageTintColor in USS
            // You'd need to adjust the material or shader of the image asset used or manage color tinting through other means
            fastBackwardButton.style.backgroundImage = new StyleBackground(fastBackwardIcon);
            //set the color of the button to be "#3FA2F6"
            //fastBackwardButton.style.unityBackgroundImageTintColor = new Color(0.2f, 0.2275f, 0.451f);
            fastBackwardButton.style.unityBackgroundImageTintColor = Color.blue;
        }
        fastBackwardButton.RegisterCallback<ClickEvent>(evt =>
        {
            FastBackward();
        });

        backButton = rootElement.Q<Button>("back_button_1");
        backButton2 = rootElement.Q<Button>("back_button_2");
        if (backButton != null)
        {
            //backButton.style.backgroundImage = new StyleBackground(backButtonIcon);
            backButton.style.unityBackgroundImageTintColor = Color.blue;
            backButton.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Back button clicked");
                OnClickBackButton();
                // Apply a style change to enlarge the button
                backButton.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1.2f)); // 20% larger

                // Optionally, you can reset the scale after a delay
                StartCoroutine(ResetScale(backButton, 0.1f)); // Reset after 0.1 seconds
            });
        }
        if (backButton2 != null)
        {
            //backButton2.style.backgroundImage = new StyleBackground(backButtonIcon);
            backButton2.style.unityBackgroundImageTintColor = Color.blue;
            backButton2.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Back button 2 clicked");
                OnClickBackButton();
                backButton2.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1.2f)); // 20% larger

                // Optionally, you can reset the scale after a delay
                StartCoroutine(ResetScale(backButton2, 0.05f)); // Reset after 0.1 seconds
            });
        }




        frameSlider = rootElement.Q<Slider>("Slider");
        // Set the slider range
        frameSlider.lowValue = 0;
        frameSlider.highValue = 100f;
        // Listen to value changes (when the user moves the slider)
        frameSlider.RegisterValueChangedCallback(evt =>
        {
            if (!isUpdatingFromCode)
            {
                int percentage = Mathf.FloorToInt(evt.newValue);
                JumpToFrame(percentage);
            }
        });
    }

    // Coroutine to reset the button scale
    private IEnumerator ResetScale(Button button, float delay)
    {
        yield return new WaitForSeconds(delay);
        button.style.scale = new Scale(Vector3.one); // Reset to original size
    }

    void JumpToFrame(int percentage)
    {
        // Implement your logic to change the content to the specified frame
        //Debug.Log($"Jumping to frame: {frameIndex}");
        int frame = Mathf.Max(Mathf.FloorToInt((float)percentage / 100 * frames.Count)-1, 0);
        frameIndex = frame;
        RenderFrame();
    }

    void JumpToTS(ClickEvent evt, int TSFrame)
    {
        frameIndex = TSFrame;
        RenderFrame();
    }

    void UpdateProgressBar(float progress)
    {
        if (frameSlider != null)
        {
            StartCoroutine(UpdateSliderValue(progress));
        }
    }

    IEnumerator UpdateSliderValue(float progress)
    {
        isUpdatingFromCode = true;
        frameSlider.value = progress;
        yield return null; // Wait for one frame to ensure the value is processed
        isUpdatingFromCode = false;
    }

    void UpdateIcon(bool isPlaying)
    {
        var playIcon512 = Resources.Load<Texture2D>("icons/play-512");
        var pauseIcon512 = Resources.Load<Texture2D>("icons/pause-512");
        var icon = isPlaying ? pauseIcon512 : playIcon512;
        playPauseToggle.style.backgroundImage = new StyleBackground(icon);
        playPauseToggle.style.unityBackgroundImageTintColor = Color.blue;
    }

    public void Pause()
    {
        // Stop the loop when the button is clicked
        isPlaying = !isPlaying;
    }

    public void FastForward()
    {
        int skipAmount = Mathf.Max(1, frames.Count / 10); // Example: Skip 1/50th of the total frames
        frameIndex = (frameIndex + skipAmount) % frames.Count;
        RenderFrame();
    }

    public void FastBackward()
    {
        int skipAmount = Mathf.Max(1, frames.Count / 10); // Ensure at least one frame is skipped
        frameIndex = ((frameIndex - skipAmount) % frames.Count + frames.Count) % frames.Count;
        RenderFrame();
    }


    public void SpeedUp2X()
    {
        // Stop the loop when the button is clicked
        speed = 4f;
    }

    public void SpeedUphalfX()
    {
        // Stop the loop when the button is clicked
        speed = 1f;
    }

    public int GetPercentage()
    {
        double percentage = (double)currentIndex / readText.Count * 100;
        return (int)Math.Round(percentage);
    }

    private void StartInertia(float delta)
    {
        scrollVelocity = delta;
        isInertiaActive = true;
    }

    private void StopInertia()
    {
        scrollVelocity = 0;
        isInertiaActive = false;
    }

    public void CollapseAllFoldouts()
    {
        foreach (var foldout in allFoldouts)
        {
            foldout.value = false;
        }
    }

    public void ExpandAllFoldouts()
    {
        foreach (var foldout in allFoldouts)
        {
            foldout.value = true;
        }
    }
}
