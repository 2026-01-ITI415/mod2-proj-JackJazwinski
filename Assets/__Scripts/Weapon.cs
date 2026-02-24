using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary
/// This is an enum of the various possible weapon types.
/// It also includes a "shield" type to allow a shield PowerUp.
/// Items marked [NI] below are Not Implemented in this book.
/// </summary
public enum eWeaponType
{
    none,       // The default / no weapon
    blaster,    // A simple blaster
    spread,     // Multiple shots simultaneously
    phaser,     // [NI] Shots that move in waves
    missile,    // [NI] Homing missiles

    laser,      // [NI] Damage over time
    shield,     // Raise shieldLevel
    rapid,      // Temporary faster firing
    speed       // Temporary movement boost
}


/// <summary
/// The WeaponDefinition class allows you to set the properties
///   of a specific weapon in the Inspector. The Main class has
///   an array of WeaponDefinitions that makes this possible.
/// </summary
[System.Serializable]                                                         // a
public class WeaponDefinition
{                                               // b
    public eWeaponType type = eWeaponType.none;
    [Tooltip("Letter to show on the PowerUp Cube")]                           // c
    public string letter;
    [Tooltip("Color of PowerUp Cube")]
    public Color powerUpColor = Color.white;                           // d
    [Tooltip("Prefab of Weapon model that is attached to the Player Ship")]
    public GameObject weaponModelPrefab;
    [Tooltip("Prefab of projectile that is fired")]
    public GameObject projectilePrefab;
    [Tooltip("Color of the Projectile that is fired")]
    public Color projectileColor = Color.white;                        // d
    [Tooltip("Damage caused when a single Projectile hits an Enemy")]
    public float damageOnHit = 0;
    [Tooltip("Damage caused per second by the Laser [Not Implemented]")]
    public float damagePerSec = 0;
    [Tooltip("Seconds to delay between shots")]
    public float delayBetweenShots = 0;
    [Tooltip("Velocity of individual Projectiles")]
    public float velocity = 50;
}

public class Weapon : MonoBehaviour
{
    static public Transform PROJECTILE_ANCHOR;

    [Header("Dynamic")]                                                        // a
    [SerializeField]                                                           // a
    [Tooltip("Setting this manually while playing does not work properly.")]   // a
    private eWeaponType _type = eWeaponType.none;
    public WeaponDefinition def;
    public float nextShotTime; // Time the Weapon will fire next

    private GameObject weaponModel;
    private Transform shotPointTrans;
    private LineRenderer laserLine;
    private float laserVisibleUntil = -1f;
    private const float LASER_VISUAL_PERSIST = 0.06f;
    private const float LASER_BEAM_LENGTH = 100f;
    private const float LASER_BEAM_WIDTH = 0.22f;

    void Start()
    {
        // Set up PROJECTILE_ANCHOR if it has not already been done
        if (PROJECTILE_ANCHOR == null)
        {                                       // b
            GameObject go = new GameObject("_ProjectileAnchor");
            PROJECTILE_ANCHOR = go.transform;
        }

        shotPointTrans = transform.GetChild(0);                              // c

        // Call SetType() for the default _type set in the Inspector
        SetType(_type);                                                      // d

        // Find the fireEvent of a Hero Component in the parent hierarchy
        Hero hero = GetComponentInParent<Hero>();                              // e
        if (hero != null) hero.fireEvent += Fire;
    }

    void Update()
    {
        if (laserLine == null) return;

        // Hide the beam shortly after firing stops, so it appears continuous
        // while the fire button is held.
        bool shouldShow = (type == eWeaponType.laser) && (Time.time < laserVisibleUntil);
        if (laserLine.enabled != shouldShow)
        {
            laserLine.enabled = shouldShow;
        }

        if (shouldShow)
        {
            Vector3 start = shotPointTrans.position;
            start.z = 0;
            Vector3 end = start + (Vector3.up * LASER_BEAM_LENGTH);
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
        }
    }

    public eWeaponType type
    {
        get { return (_type); }
        set { SetType(value); }
    }

    public void SetType(eWeaponType wt)
    {
        _type = wt;
        if (type == eWeaponType.none)
        {                                       // f
            this.gameObject.SetActive(false);
            return;
        }
        else
        {
            this.gameObject.SetActive(true);
        }
        // Get the WeaponDefinition for this type from Main
        def = Main.GET_WEAPON_DEFINITION(_type);
        // Destroy any old model and then attach a model for this weapon     // g
        if (weaponModel != null) Destroy(weaponModel);
        weaponModel = Instantiate<GameObject>(def.weaponModelPrefab, transform);
        weaponModel.transform.localPosition = Vector3.zero;
        weaponModel.transform.localScale = Vector3.one;
        if (laserLine != null)
        {
            Destroy(laserLine.gameObject);
            laserLine = null;
        }

        nextShotTime = 0; // You can fire immediately after _type is set.    // h
    }

    private void Fire()
    {
        // If this.gameObject is inactive, return
        if (!gameObject.activeInHierarchy) return;                         // i
        // If it hasn’t been enough time between shots, return
        if (Time.time < nextShotTime) return;                              // j

        ProjectileHero p;
        Vector3 vel = Vector3.up * def.velocity;

        switch (type)
        {                                                      // k
            case eWeaponType.blaster:
                p = MakeProjectile();
                p.vel = vel;
                break;

            case eWeaponType.spread:                                         // l
                p = MakeProjectile();
                p.vel = vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(10, Vector3.back);
                p.vel = p.transform.rotation * vel;
                p = MakeProjectile();
                p.transform.rotation = Quaternion.AngleAxis(-10, Vector3.back);
                p.vel = p.transform.rotation * vel;
                break;

            case eWeaponType.laser:
                FireLaserBeam();
                break;

        }
    }

    private void FireLaserBeam()
    {
        if (laserLine == null)
        {
            GameObject beamGO = new GameObject("_LaserBeam");
            beamGO.transform.SetParent(PROJECTILE_ANCHOR);
            laserLine = beamGO.AddComponent<LineRenderer>();
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
            laserLine.positionCount = 2;
            laserLine.useWorldSpace = true;
            laserLine.widthMultiplier = LASER_BEAM_WIDTH;
            laserLine.numCapVertices = 4;
            laserLine.enabled = false;
        }

        // Keep the beam world-aligned so it always points straight up.
        Color beamColor = def.projectileColor;
        beamColor.a = 0.95f;
        laserLine.startColor = beamColor;
        laserLine.endColor = beamColor;

        Vector3 start = shotPointTrans.position;
        start.z = 0;
        Vector3 end = start + (Vector3.up * LASER_BEAM_LENGTH);
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
        laserLine.enabled = true;

        float fireDelayMultiplier = 1f;
        if (Hero.S != null)
        {
            fireDelayMultiplier = Hero.S.FireDelayMultiplier;
        }

        float currDelay = def.delayBetweenShots * fireDelayMultiplier;
        laserVisibleUntil = Time.time + Mathf.Max(LASER_VISUAL_PERSIST, currDelay + 0.02f);

        // Laser uses damage-per-second if configured, otherwise falls back.
        // If delay is zero, use frame delta so DPS still applies.
        float tickSeconds = currDelay > 0 ? currDelay : Time.deltaTime;
        float damage = def.damagePerSec > 0 ? def.damagePerSec * tickSeconds : def.damageOnHit;
        if (damage > 0)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                start,
                Vector3.up,
                LASER_BEAM_LENGTH,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );
            for (int i = 0; i < hits.Length; i++)
            {
                Enemy e = hits[i].collider.transform.root.GetComponent<Enemy>();
                if (e == null) continue;

                e.health -= damage;
                if (e.health <= 0)
                {
                    Main.SHIP_DESTROYED(e);
                    Destroy(e.gameObject);
                }
            }
        }
        nextShotTime = Time.time + currDelay;
    }

    private ProjectileHero MakeProjectile()
    {                                 // m
        GameObject go;
        go = Instantiate<GameObject>(def.projectilePrefab, PROJECTILE_ANCHOR); // n
        ProjectileHero p = go.GetComponent<ProjectileHero>();

        Vector3 pos = shotPointTrans.position;
        pos.z = 0;                                                            // o
        p.transform.position = pos;

        p.type = type;
        // Hero power-ups can modify weapon fire delay globally.
        float fireDelayMultiplier = 1f;
        if (Hero.S != null)
        {
            fireDelayMultiplier = Hero.S.FireDelayMultiplier;
        }
        nextShotTime = Time.time + (def.delayBetweenShots * fireDelayMultiplier); // p
        return (p);
    }
}


