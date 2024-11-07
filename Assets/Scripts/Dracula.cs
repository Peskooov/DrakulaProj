using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[RequireComponent(typeof(InteractiveObject))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PathBuilder))]
public class Dracula : SingletonBase<Dracula>
{
    [SerializeField] private bool playOnAwake = true;
    [Space][Header("Dracula Prefabs")]
    [SerializeField] private GameObject draculaPrefabsNone;
    [SerializeField] private GameObject draculaPrefabsSexy;
    [SerializeField] private GameObject draculaPrefabsCross;
    [SerializeField] private GameObject draculaPrefabsStand;
    [SerializeField] private GameObject draculaPrefabsFly;
    [SerializeField] private GameObject draculaPrefabsHand;
    
    [Space][Header("Visual Prefabs")]
    [SerializeField] private DraculaSpawnEffect draculaSpawnEffectPrefab;
    [SerializeField] private ImpactEffect visionEffectPrefab;

    [Space] [Header("Dracula Settings")] 
    [SerializeField] [Range(0.2f, 30f)]private float spawnSpeed = 7;
    [SerializeField] [Range(0f, 10f)]private int speedChange = 2;
    [SerializeField] [Range(0f, 50f)] private int minDistanceToNextPp = 6;
    [SerializeField] private AudioClip[] spawnClips;
    [SerializeField] private DraculaPoint[] spawnPositions;
    
    private Transform character;
    private DraculaPoint draculaPoint;
    private DraculaPoint playerPoint;
    private GameObject draculaPrefab;
    private AudioSource source;
    private MeshRenderer draculaMeshRenderer;
    private DraculaSpawnEffect draculaSpawnEffect;
    private PathBuilder builder;
    private float timer;
    
    
    private bool isHeart = false;
    private bool isVisible = false;
    private bool isSpawning = false;

    [HideInInspector] public UnityEvent<int> draculaInPlayer;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        CharacterInputController.Instance.heartOn.AddListener(TogleHeartOn);
        CharacterInputController.Instance.heartOff.AddListener(TogleHeartOff);
        GetComponent<InteractiveObject>().onVision.AddListener(TogleVisionOn);
        GetComponent<InteractiveObject>().onHide.AddListener(TogleVisionOff);
        
        NoiseLevel.Instance.OnChange += SpeedChange;
        
        character = Character.Instance.transform;
        source = GetComponent<AudioSource>();
        builder = GetComponent<PathBuilder>();
        playerPoint = character.GetComponent<DraculaPoint>();
        draculaPoint = GetComponent<DraculaPoint>();

        if (playOnAwake)
        {
            SetPoint(spawnPositions[Random.Range(0, spawnPositions.Length)]);
        }
        else
        {
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        CharacterInputController.Instance.draculaAnim.RemoveListener(TogleHeartOn);
        CharacterInputController.Instance.draculaAnim.RemoveListener(TogleHeartOff);
        GetComponent<InteractiveObject>().onVision.RemoveListener(TogleVisionOn);
        GetComponent<InteractiveObject>().onHide.RemoveListener(TogleVisionOff);
        NoiseLevel.Instance.OnChange -= SpeedChange;
    }

    private int lastValue = 0;
    public void SpeedChange(int value)
    {
        if (lastValue > value) spawnSpeed += speedChange;
        else if (spawnSpeed - speedChange >= 0 )spawnSpeed -= speedChange;
        lastValue = value;
    }

    private void FixedUpdate()
    {
        if (draculaSpawnEffect != null && draculaSpawnEffect.IsPlaying())
        {
            DraculaState();
            return;
        }
        
        timer += Time.deltaTime;
        
        if (timer >= spawnSpeed)
        {
            DraculaMove();
            timer = 0;
        }
        DraculaState();
    }

    private void DraculaState()
    {
        VisibleMeshDracula();
        DraculaRotateToPlayer();
        DraculaEffect();
    }
    
    private void TogleVisionOn() => isVisible = true;
    private void TogleVisionOff() => isVisible = false;
    private void TogleHeartOn() => isHeart = true;
    private void TogleHeartOff() => isHeart = false;

    private bool isActiveMesh;
    private void VisibleMeshDracula()
    {
        if (draculaMeshRenderer != null)
        {
            if (isVisible && isHeart || isHeart && isActiveMesh)
            {
                if (draculaMeshRenderer.enabled == false)
                {
                    isActiveMesh = true;
                    draculaMeshRenderer.enabled = true;
                    Instantiate(visionEffectPrefab,transform.position,Quaternion.identity);
                }
            }
            else
            {
                if (draculaMeshRenderer.enabled == true)
                {
                    isActiveMesh = false;
                    draculaMeshRenderer.enabled = false;
                    Instantiate(visionEffectPrefab,transform.position,Quaternion.identity);
                }
            }
        }
    } 
    private void DraculaEffect()
    {
        if (isVisible && isHeart)
        {
            if (draculaSpawnEffect == null)
            {
                draculaSpawnEffect = Instantiate(draculaSpawnEffectPrefab,new Vector3(transform.position.x,transform.position.y,transform.position.z), Quaternion.identity);
            }
            else if (!draculaSpawnEffect.IsPlaying())
            {
                draculaSpawnEffect = Instantiate(draculaSpawnEffectPrefab,new Vector3(transform.position.x,transform.position.y,transform.position.z), Quaternion.identity);
            }
        }
    }
    private void DraculaRotateToPlayer() 
    {
        if (draculaPrefab != null)
        {
            draculaPrefab.transform.LookAt(new Vector3(character.position.x,transform.position.y,character.position.z));
        }
    }
    public void RandomPoint()
    {
        DraculaPoint rand = spawnPositions[Random.Range(0, spawnPositions.Length)];
        transform.position = rand.transform.position;
        Spawn(rand);
    }
    
    public void SetPoint(DraculaPoint spawnPoint)
    {
        transform.position = spawnPoint.transform.position;
        draculaPoint = spawnPoint;
        Spawn(spawnPoint);
    }
    
    public void SetPoints(DraculaPoint[] spawnPoints)
    {
        spawnPositions = spawnPoints;
        DraculaPoint rand = spawnPositions[Random.Range(0, spawnPositions.Length)];
        draculaPoint = rand;
        Spawn(rand);
    }
    
    private void Spawn(DraculaPoint spawnPoint)
    {
        isSpawning = true;
        source.PlayOneShot(spawnClips[Random.Range(0,spawnClips.Length)]);
        draculaPrefab = Instantiate(GetDraculaPrefab(spawnPoint), transform.position, Quaternion.identity, transform);
        draculaMeshRenderer = draculaPrefab.GetComponent<MeshRenderer>();
        draculaMeshRenderer.enabled = false;
        enabled = true;
    }

    public void DraculaEnable()
    {
        if (isSpawning)
        {
            transform.position = lastPosition;
            enabled = true;
        }
    }
    
    private Vector3 lastPosition;
    public void DraculaDisable()
    {
        lastPosition = transform.position;
        transform.position = Vector3.zero;
        timer = 0;
        enabled = false;
    }
    public void DraculaDespawn()
    {
        DraculaDisable();
        builder.ClearPath();
        isSpawning = false;
    }
    
    private void DraculaMove()
    {
        Destroy(draculaPrefab);
        
        var movePoint = builder.GetDraculaPoint(draculaPoint,playerPoint,minDistanceToNextPp);

        if (movePoint == null) return;
        
        if (movePoint.IsPlayer)
        {
            KillPlayer();
            enabled = false;
            return;
        }

        draculaPrefab = Instantiate(GetDraculaPrefab(movePoint), movePoint.transform.position, Quaternion.identity, transform);
        draculaMeshRenderer = draculaPrefab.GetComponent<MeshRenderer>();
        draculaMeshRenderer.enabled = false;
        draculaPoint = movePoint;
    }

    private GameObject GetDraculaPrefab(DraculaPoint draculaPoint)
    {
        var currentDraculaPrefab = draculaPrefabsNone;
        var currentPoint = draculaPoint.DraculaPos;
        
        if (currentPoint == DraculaPosType.None && currentDraculaPrefab != null)
        {
            var rand = Random.Range(0,3);
            if (rand == 0) currentPoint = DraculaPosType.Fly;
            if (rand == 1) currentPoint = DraculaPosType.Stand;
            if (rand == 2) currentPoint = DraculaPosType.Cross;
            if (rand == 3) currentPoint = DraculaPosType.Hand ; 
        }
        
        if (currentPoint == DraculaPosType.Sexy 
            && draculaPrefabsSexy != null) currentDraculaPrefab = draculaPrefabsSexy;
        if (currentPoint == DraculaPosType.Stand 
            && draculaPrefabsStand != null) currentDraculaPrefab = draculaPrefabsStand;
        if (currentPoint == DraculaPosType.Cross 
            && draculaPrefabsCross != null) currentDraculaPrefab = draculaPrefabsCross;
        if (currentPoint == DraculaPosType.Hand 
            && draculaPrefabsHand != null) currentDraculaPrefab = draculaPrefabsHand;
        if (currentPoint == DraculaPosType.Fly 
            && draculaPrefabsFly != null) currentDraculaPrefab = draculaPrefabsFly;
        transform.position = draculaPoint.transform.position;

        return currentDraculaPrefab;
    }
    private void KillPlayer()
    {
        draculaInPlayer.Invoke(1);
        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistanceToNextPp);
    }

}