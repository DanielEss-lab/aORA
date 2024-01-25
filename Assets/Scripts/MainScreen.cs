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
using static Unity.VisualScripting.Metadata;
using System;

[System.Serializable]
public class Reaction
{
    public string category;
    public string reactionName;
    public string filename;
    public int transitionState;
}

[System.Serializable]
public class ReactionsList
{
    public List<Reaction> reactions;
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



    //Reaction stuff
    private GameObject curReaction;
    private GameObject curBond;
    private GameObject wholeReaction;
    public Dictionary<string, Transform> Children = new Dictionary<string, Transform>();
    private Dictionary<int, String> AtomNames = new Dictionary<int, String>();

    private String pattern = @"^(\s+-?\d+\.?\d+){3}\s+\w{1,2}";
    private String bondpattern = @"^(\s*([0-9]*\.[0-9]+|[0-9]+)\s*){3}$";
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
    private int framecounter = 0;
    private int currentPanelIndex;
    private int currentIndex = 0;
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
                var button = new Button(() => ButtonClicked(reaction.filename))
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
        toggleButton.label = toggleButton.value ? "Collapse All" : "Expand All";
        toggleButton.RegisterValueChangedCallback(evt =>
        {
            toggleButton.label = evt.newValue ? "Collapse All" : "Expand All";
        });
        toggleButton.RegisterCallback<ClickEvent>(evt =>
        {
            toggleButton.label = toggleButton.value ? "Collapse All" : "Expand All";
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
    }



    private void ButtonClicked(string filename)
    {
        Debug.Log($"Clicked Button {filename}");
        uiPage.visible = false;
        reactionPage.visible = true;
        Children = new Dictionary<string, Transform>();
        AtomNames = new Dictionary<int, String>();
        currentIndex = 0;
        framecounter = 0;
        curReaction = InstantiatePrefabByName(filename);
    }

    GameObject InstantiatePrefabByName(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab != null)
        {
            //GameObject instantiatedPrefab = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            GameObject instantiatedPrefab = Instantiate(prefab, fixedPostion, transform.rotation);
            // Optionally, set the instantiated prefab as a child of another GameObject
            // instantiatedPrefab.transform.SetParent(someParentTransform);

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
