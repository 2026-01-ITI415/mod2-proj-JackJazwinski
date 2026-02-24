using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : MonoBehaviour
{

    static public Hero S { get; private set; }  // Singleton property    // a

    [Header("Inscribed")]
    // These fields control the movement of the ship
    public float speed = 30;
    public float rollMult = -45;
    public float pitchMult = 30;
    public GameObject projectilePrefab;
    public float projectileSpeed = 40;
    public Weapon[] weapons;
    [Header("PowerUp Effects")]
    public float rapidFireMultiplier = 0.5f;
    public float rapidFireDuration = 6f;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 6f;
    public float laserDuration = 10f;

    [Header("Dynamic")]
    [Range(0, 4)]
    [SerializeField]                                        // b
    private float _shieldLevel = 1;

    [Tooltip("This field holds a reference to the last triggering GameObject")]
    private GameObject lastTriggerGo = null;
    private float rapidFireEndTime = -1f;
    private float speedEndTime = -1f;
    private float laserEndTime = -1f;
    private eWeaponType[] preLaserWeaponLoadout;

    // Declare a new delegate type WeaponFireDelegate
    public delegate void WeaponFireDelegate();                                // a     // Create a WeaponFireDelegate event named fireEvent.
    public event WeaponFireDelegate fireEvent;



    void Awake()
    {
        if (S == null)
        {
            S = this; // Set the Singleton only if it’s null                  // c
        }
        else
        {
            Debug.LogError("Hero.Awake() - Attempted to assign second Hero.S!");
        }
        //fireEvent += TempFire;

        // Reset the weapons to start _Hero with 1 blaster
        ClearWeapons();
        weapons[0].SetType(eWeaponType.blaster);
    }

    void Update()
    {
        // Pull in information from the Input class
        float hAxis = Input.GetAxis("Horizontal");                            // d
        float vAxis = Input.GetAxis("Vertical");                              // d

        // Change transform.position based on the axes
        Vector3 pos = transform.position;
        float currSpeed = speed * MoveSpeedMultiplier;
        pos.x += hAxis * currSpeed * Time.deltaTime;
        pos.y += vAxis * currSpeed * Time.deltaTime;
        transform.position = pos;

        // Rotate the ship to make it feel more dynamic                       // e
        transform.rotation = Quaternion.Euler(vAxis * pitchMult, hAxis * rollMult, 0);

        // Allow the ship to fire
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    TempFire();
        //}

        // Use the fireEvent to fire Weapons when the Spacebar is pressed.
        if (Input.GetAxis("Jump") == 1 && fireEvent != null)
        {
            fireEvent();
        }

        // End laser once its timer has fully expired.
        if (laserEndTime > 0f && Time.time >= laserEndTime)
        {
            EndLaserAndRestorePreviousWeapons();
        }

    }


    //void TempFire()
    //{
    //    GameObject projGO = Instantiate<GameObject>(projectilePrefab);
    //    projGO.transform.position = transform.position;
    //    Rigidbody rigidB = projGO.GetComponent<Rigidbody>();
    //    //rigidB.velocity = Vector3.up * projectileSpeed;

    //    ProjectileHero proj = projGO.GetComponent<ProjectileHero>();         // h
    //    proj.type = eWeaponType.blaster;
    //    float tSpeed = Main.GET_WEAPON_DEFINITION(proj.type).velocity;
    //    rigidB.velocity = Vector3.up * tSpeed;

    //}

    void OnTriggerEnter(Collider other)
    {
        Transform rootT = other.gameObject.transform.root;                    // a
        GameObject go = rootT.gameObject;
        //Debug.Log("Shield trigger hit by: " + go.gameObject.name);

        // Make sure it’s not the same triggering go as last time
        if (go == lastTriggerGo) return;                                    // c
        lastTriggerGo = go;                                                   // d

        Enemy enemy = go.GetComponent<Enemy>();                               // e
        PowerUp pUp = go.GetComponent<PowerUp>();

        if (enemy != null)
        {  // If the shield was triggered by an enemy
            shieldLevel--;        // Decrease the level of the shield by 1
            Destroy(go);          // … and Destroy the enemy                  // f
        }
        else if (pUp != null)
        {
            AbsorbPowerUp(pUp);
        }
        else
        {
            Debug.LogWarning("Shield trigger hit by non-Enemy: " + go.name);    // g
        }
    }

    public float shieldLevel
    {
        get { return (_shieldLevel); }                                      // b
        private set
        {                                                         // c
            _shieldLevel = Mathf.Min(value, 4);                             // d
            // If the shield is going to be set to less than zero…
            if (value < 0)
            {                                                  // e
                Destroy(this.gameObject);  // Destroy the Hero
                Main.HERO_DIED();
            }
        }
    }

    public float FireDelayMultiplier
    {
        get
        {
            if (Time.time < rapidFireEndTime) return rapidFireMultiplier;
            return 1f;
        }
    }

    private float MoveSpeedMultiplier
    {
        get
        {
            if (Time.time < speedEndTime) return speedMultiplier;
            return 1f;
        }
    }

    private bool IsLaserActive
    {
        get
        {
            return laserEndTime > Time.time && laserEndTime > 0f;
        }
    }

    private bool IsWeaponPowerUpType(eWeaponType wt)
    {
        return wt != eWeaponType.none
            && wt != eWeaponType.shield
            && wt != eWeaponType.rapid
            && wt != eWeaponType.speed;
    }

    private void ActivateLaser()
    {
        if (!IsLaserActive)
        {
            preLaserWeaponLoadout = new eWeaponType[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                preLaserWeaponLoadout[i] = weapons[i].type;
            }
        }
        laserEndTime = Time.time + laserDuration;
        ClearWeapons();
        weapons[0].SetType(eWeaponType.laser);
    }

    private void EndLaserAndRestorePreviousWeapons()
    {
        laserEndTime = -1f;
        ClearWeapons();
        if (preLaserWeaponLoadout == null) return;
        for (int i = 0; i < weapons.Length && i < preLaserWeaponLoadout.Length; i++)
        {
            weapons[i].SetType(preLaserWeaponLoadout[i]);
        }
    }

    /// <summary>
    /// Finds the first empty Weapon slot (i.e., type=none) and returns it.
    /// </summary>
    /// <returnsThe first empty Weapon slot or null if none are empty</returns
    Weapon GetEmptyWeaponSlot()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == eWeaponType.none)
            {
                return (weapons[i]);
            }
        }
        return (null);
    }

    /// <summary>
    /// Sets the type of all Weapon slots to none
    /// </summary>
    void ClearWeapons()
    {
        foreach (Weapon w in weapons)
        {
            w.SetType(eWeaponType.none);
        }
    }

    public void AbsorbPowerUp(PowerUp pUp)
    {
        Debug.Log("Absorbed PowerUp: " + pUp.type);                         // b
        switch (pUp.type)
        {
            case eWeaponType.shield:                                              // a 
                shieldLevel++;
                break;
            case eWeaponType.laser:
                ActivateLaser();
                break;
            case eWeaponType.rapid:
                rapidFireEndTime = Time.time + rapidFireDuration;
                break;
            case eWeaponType.speed:
                speedEndTime = Time.time + speedDuration;
                break;

            default:                                                             // b
                if (IsLaserActive && IsWeaponPowerUpType(pUp.type))
                {
                    // Picking up a different weapon interrupts laser immediately.
                    laserEndTime = -1f;
                    preLaserWeaponLoadout = null;
                }
                if (pUp.type == weapons[0].type)
                { // If it is the same type     // c
                    Weapon weap = GetEmptyWeaponSlot();
                    if (weap != null)
                    {
                        // Set it to pUp.type
                        weap.SetType(pUp.type);
                    }
                }
                else
                { // If this is a different weapon type                   // d
                    ClearWeapons();
                    weapons[0].SetType(pUp.type);
                }
                break;

        }
        pUp.AbsorbedBy(this.gameObject);
    }

}
