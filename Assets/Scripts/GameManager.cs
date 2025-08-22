using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{


    public GameObject planetModel;

    //  Time scale controls
    public float changeMultiplicator = 4;
    float lastSpeed = 1;

    public InputActionAsset cameraControls;
    private InputAction speedUpAction;
    private InputAction slowDownAction;
    private InputAction pauseAction;


    #region Singleton
    private static GameManager _instance;
    public static GameManager gameManager   //  Singleton for Game Manager
    {
        get
        {
            _instance = GameObject.FindObjectOfType<GameManager>();
            if (_instance == null)
            {
                Debug.LogError("No Game Manager found!");
            }
            //DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }
    #endregion

    void Awake()
    {
        // Find the actions within the "Game" action map
        InputActionMap gameActionMap = cameraControls.FindActionMap("Game");
        speedUpAction = gameActionMap.FindAction("SpeedUp");
        slowDownAction = gameActionMap.FindAction("SlowDown");
        pauseAction = gameActionMap.FindAction("Pause");

        // Subscribe to the 'performed' event for each action
        speedUpAction.performed += OnSpeedUp;
        slowDownAction.performed += OnSlowDown;
        pauseAction.performed += OnPause;

        // Enable the actions
        speedUpAction.Enable();
        slowDownAction.Enable();
        pauseAction.Enable();
    }



    void Start()
    {
        
    }


    void Update()
    {
        
    }

    public void SetViewMode(int modeToSet)
    {
        FindObjectOfType<GlobeDisplayManager>().SetViewMode((GlobeViewMode)modeToSet);
    }

    #region Game Time
    void OnSpeedUp(InputAction.CallbackContext context)
    {
        //  Limit at 256
        if (CustomTime.timeScale >= 256)
            ApplyTimeScale(256);
        //  If paused - set to 1
        else if (CustomTime.timeScale <= 0)
            ApplyTimeScale(1);
        //  Normally - multiply by 4
        else
            ApplyTimeScale(CustomTime.timeScale * changeMultiplicator);

    }
    void OnSlowDown(InputAction.CallbackContext context)
    {
        //  If at 1 or lower (paused) - pause
        if (CustomTime.timeScale <= 1)
            ApplyTimeScale(0);
        //  Normally - divide by 4
        else
            ApplyTimeScale(CustomTime.timeScale / changeMultiplicator);


    }
    void OnPause(InputAction.CallbackContext context)
    {
        //  If not paused
        if (CustomTime.timeScale != 0)
        {
            //  Save time scale and set to 0
            lastSpeed = CustomTime.timeScale;
            ApplyTimeScale(0);
        }
        //  If paused
        else
        {
            //  Apply last saved speed if saved
            if (lastSpeed != 0)
                ApplyTimeScale(lastSpeed);
            //  If it was saved as 0 - set to 1
            else
                ApplyTimeScale(1);
        }
    }


    void ApplyTimeScale(float scaleValue)
    {
        Debug.Log("Setting time scale to " + scaleValue);
        CustomTime.timeScale = scaleValue;
    }

    #endregion

}
