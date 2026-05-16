using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingScene : MonoBehaviour
{
    public static LoadingScene Instance;

    [SerializeField] private ShaderVariantCollection shaderCollection;
    private UIDocument uiDocument;
    private VisualElement loadingScreen;
    private VisualElement skullContainer;
    private VisualElement skullBackground;
    private VisualElement skullFilled;

    private string sceneToLoad = "Main Menu"; 
    private bool isCurrentlyLoading = false; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshUIReferences();
        }
        else
        {
            Instance.RefreshUIReferences(GetComponent<UIDocument>());
            Destroy(gameObject);
            return;
        }
    }

    private void RefreshUIReferences(UIDocument customDoc = null)
    {
        uiDocument = (customDoc != null) ? customDoc : Object.FindFirstObjectByType<UIDocument>();

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            if (root != null)
            {
                loadingScreen = root.Q<VisualElement>("root");
                skullContainer = root.Q<VisualElement>("SkullContainer");
                skullBackground = root.Q<VisualElement>("SkullBackground");
                skullFilled = root.Q<VisualElement>("SkullFilled");
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Loading" && !isCurrentlyLoading)
        {
            RefreshUIReferences();
            LoadGameScene(sceneToLoad); 
        }
    }

    public void PrepareAndLoad(string targetSceneName)
    {
        if (isCurrentlyLoading) return;

        sceneToLoad = targetSceneName;
        SceneManager.LoadScene("Loading");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void SyncSkullSizes()
    {
        if (skullBackground != null && skullFilled != null)
        {
            float width = skullBackground.layout.width;
            float height = skullBackground.layout.height;

            if (float.IsNaN(width) || width <= 0) width = skullBackground.resolvedStyle.width;
            if (float.IsNaN(height) || height <= 0) height = skullBackground.resolvedStyle.height;

            skullFilled.style.width = width;
            skullFilled.style.height = height;
        }
    }

    public void LoadGameScene(string targetSceneName)
    {
        if (!isCurrentlyLoading)
        {
            StartCoroutine(LoadRoutine(targetSceneName));
        }
    }

    private IEnumerator LoadRoutine(string targetSceneName)
    {
        isCurrentlyLoading = true;
        Time.timeScale = 1f; // Zapewnia, że czas płynie nawet po wyjściu z pauzy

        if (loadingScreen != null) loadingScreen.style.display = DisplayStyle.Flex;
        if (skullContainer != null) skullContainer.style.height = Length.Percent(0);
        
        yield return null; 
        SyncSkullSizes();

        // --- ETAP 1: SHADERY (0% -> 95%) ---
        float shaderWeight = 95f; 
        if (shaderCollection != null)
        {
            shaderCollection.WarmUp();
            
            float elapsed = 0f;
            float duration = 1.0f; 
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Używamy czasu niezależnego od TimeScale
                float currentPercent = (elapsed / duration) * shaderWeight;
                
                if (skullContainer != null)
                    skullContainer.style.height = Length.Percent(currentPercent);
                
                yield return null;
            }
        }

        // --- ETAP 2: SCENA (95% -> 100%) ---
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                float sceneProgress = asyncLoad.progress / 0.9f;
                float totalProgress = shaderWeight + (sceneProgress * (100f - shaderWeight));
        
                if (skullContainer != null)
                    skullContainer.style.height = Length.Percent(totalProgress);
                
                yield return null;
            }
            
            if (skullContainer != null) skullContainer.style.height = Length.Percent(100f);
            
            yield return new WaitForSeconds(0.5f);
            asyncLoad.allowSceneActivation = true;
            yield return new WaitForSeconds(0.1f);
            
            if (loadingScreen != null && loadingScreen.panel != null)
            {
                loadingScreen.style.display = DisplayStyle.None;
            }
        }
        
        isCurrentlyLoading = false;
    }
}