//using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using System.Collections;
using System.Collections.Generic;

public class Player : Singleton<Player>
{
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;
    private PlayerAnimation _playerAnimation;
    
    private Vector2 _direction;    
    private Vector2 _pointerPos;
    private WeaponParent _weaponParent;
    [SerializeField] private float _maxHealth = 100;
    private float _currentHealth;

    private HealthBar _healthBar;
    private float _healthMultiplier = 1;
    private float _speedMultiplier = 1;
    private float _strengthMultiplier = 1;

    // Start is called before the first frame update
    void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();
        _playerAnimation = GetComponent<PlayerAnimation>();
        _weaponParent = GetComponentInChildren<WeaponParent>();
        
        _playerInput.LeftClick += () => _weaponParent.Attack();

        _healthBar = FindObjectOfType<HealthBar>();
        _currentHealth = _maxHealth;
        _healthBar.SetMaxHealth(_currentHealth);
    }

    // Update handles the animation changes based on the mouse pointer 
    void Update()
    {
        _weaponParent.PointerPosition = _playerInput.PointerPosition;
        _playerAnimation.AnimatePlayer(_playerInput.MovementDirection.x, _playerInput.MovementDirection.y, _playerInput.PointerPosition, _playerMovement.GetRBPos());

    }
    
    // FixedUpdate handles the movement 
    private void FixedUpdate()
    {
        _playerMovement.MovePlayer(_playerInput.MovementDirection, _playerInput.GetShiftDown());
    }

    public void TakeDamage(float damage)
    {

        _currentHealth -= damage;
        _healthBar.SetHealth(_currentHealth);
        StartCoroutine(FlashRed());
        PlayerEvents.playerHit.Invoke();
        AudioManager.Instance.PlayPLayerHurtSound();
    }

    private IEnumerator FlashRed()
    {
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        playerRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        playerRenderer.color = Color.white;
    }

    public void SetPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void IncreaseSpeed (float increaseMultiplier)
    {
        _speedMultiplier += increaseMultiplier;
        _playerMovement.IncreaseSpeed(_speedMultiplier);
    }

    public void IncreaseHealth(float increaseMultiplier)
    {
        //TODO in future, for now not used
        _healthMultiplier += increaseMultiplier;
        _currentHealth = _currentHealth * _healthMultiplier;
        _healthBar.SetHealth(_currentHealth);
    }

    public void RegenerateHealth()
    {
        _currentHealth = _maxHealth;
        _healthBar.SetHealth(_currentHealth);
    }

    public void IncreaseStrenght(float increaseMultiplier)
    {
        _strengthMultiplier += increaseMultiplier;
    }

    public void ChangeWeapon(WeaponParent weapon)
    {
        Destroy(_weaponParent.gameObject);
        _weaponParent = Instantiate(weapon);
        _weaponParent.transform.parent = transform;
        _weaponParent.transform.localPosition = new Vector2(0.1f, 0.7f);
    }

    public void ShowDoorUI(bool show)
    {
        transform.Find("DoorUI").gameObject.SetActive(show);
    }

    public float GetStrengthMultiplier()
    {
        return _strengthMultiplier;
    }
}

