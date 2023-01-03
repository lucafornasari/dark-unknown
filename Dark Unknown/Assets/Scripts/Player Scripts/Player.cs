//using System.Numerics;

using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro;

public class Player : Singleton<Player>, IEffectable
{
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;
    private PlayerAnimation _playerAnimation;
    private SpriteRenderer _playerRenderer;

    private Vector2 _direction;
    private Vector2 _pointerPos;
    private WeaponParent _weaponParent;
    [SerializeField] private float _maxHealth = 150;
    private float _currentHealth;
    [SerializeField] private float _maxMana = 100;
    private float _currentMana;
    [SerializeField] private GameObject _playerUI;

    private StatusEffectData _statusEffect;
    private float _currentEffectTime = 0;
    private float _nextTickTime = 0;
    private GameObject _statusEffectParticles;

    private float _healthMultiplier = 1;
    private float _speedMultiplier = 1;
    private float _strengthMultiplier = 1;
    private int _killedReward = 0;

    private bool _canGetPotion;
    private bool _canGetWeapon;
    private bool _canGetAbility;
    private WeaponParent _weaponToGet;
    private Ability _abilityToGet;
    private IUsable _weaponUsable, _abilityUsable;
    private GameObject _rewardToGet;

    private bool _hasPotion;
    private Ability _ability;

    private bool _invincible;

    // Start is called before the first frame update
    private void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();
        _playerAnimation = GetComponent<PlayerAnimation>();
        _playerRenderer = GetComponent<SpriteRenderer>();

        _weaponParent = GetComponentInChildren<WeaponParent>();

        _playerInput.LeftClick += () => _weaponParent.Attack();
        //_playerInput.LeftClick += () => UIController.Instance.ClickActionButton("WeaponButton");

        _currentHealth = _maxHealth;
        UIController.Instance.SetMaxHealth(_currentHealth);
        UIController.Instance.SetSpeedMultiplierText("+ " + (_speedMultiplier - 1) * 100 + " %");
        UIController.Instance.SetStrengthMultiplierText("+ " + (_strengthMultiplier - 1) * 100 + " %");

        _currentMana = 0;
        UIController.Instance.SetMaxMana(_maxMana);
    }

    // Update handles the animation changes based on the mouse pointer
    private void Update()
    {
        if (PauseMenu.GameIsPaused) return;
        _weaponParent.PointerPosition = _playerInput.PointerPosition;
        _playerAnimation.AnimatePlayer(_playerInput.MovementDirection.x, _playerInput.MovementDirection.y,
            _playerInput.PointerPosition, _playerMovement.GetRBPos());

        if (_playerMovement.IsDashing())
        {
            return;
        }
        
        if (_canGetWeapon && InputManager.Instance.GetKeyDown(KeybindingActions.Interact))
        {
            GameObject newReward = Instantiate(_weaponParent.getWeaponReward());
            newReward.transform.position = transform.position;
            //destroy current weapon
            Destroy(_weaponParent.gameObject);
            //instantiate new current weapon
            _weaponParent = Instantiate(_weaponToGet, transform, true);
            var weaponTransform = _weaponParent.transform;
            weaponTransform.localPosition = new Vector2(0f, 0.673f);
            weaponTransform.localScale = new Vector3(1, 1, 1);
            UIController.Instance.SetUsable(UIController.Instance.actionButtons[0], _weaponUsable);
            //destroy old reward already taken
            Destroy(_rewardToGet);
            _canGetWeapon = false;
        } else if (_canGetPotion && !_hasPotion && InputManager.Instance.GetKeyDown(KeybindingActions.Interact))
        {
            //destroy potion taken game-object
            Destroy(_rewardToGet);
            UIController.Instance.SetUsable(UIController.Instance.actionButtons[2], new Potion());
            _canGetPotion = false;
            _hasPotion = true;
        } else if (_canGetAbility && InputManager.Instance.GetKeyDown(KeybindingActions.Interact))
        {
            if (_ability)
            {
                GameObject newReward = Instantiate(_ability.GetAbilityReward(), transform.position, Quaternion.identity);
                Destroy(_ability.gameObject);
            }
            _ability = Instantiate(_abilityToGet, transform, true);
            //destroy old reward already taken
            Destroy(_rewardToGet);
            UIController.Instance.SetUsable(UIController.Instance.actionButtons[1], _abilityUsable);
            _canGetAbility = false;
        }

        if(_statusEffect != null) HandleEffect();

        // Use potion only when the player has one and has lower than max health
        if (InputManager.Instance.GetKeyDown(KeybindingActions.Potion))
        {
            UIController.Instance.ClickActionButton("PotionButton");
        }

        if (InputManager.Instance.GetKeyDown(KeybindingActions.Spell))
        {
            UIController.Instance.ClickActionButton(("SpellButton"));
        }
        if (InputManager.Instance.GetKeyDown(KeybindingActions.Dash))
        {
            _playerMovement.Dash(_playerInput.MovementDirection);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            _invincible = !_invincible;
            Debug.Log("Player is invincible: " + _invincible);
        }
    }

    // FixedUpdate handles the movement
    private void FixedUpdate()
    {
        if (_playerMovement.IsDashing())
        {
            return;
        }

        _playerMovement.MovePlayer(_playerInput.MovementDirection);
    }

    public void TakeDamage(float damage)
    {
        if (_ability == null || !_ability.GetComponent<ShieldAbility>() ||
            (_ability.GetComponent<ShieldAbility>() && !_ability.IsActive()))
        {
            _currentHealth -= damage;
            UIController.Instance.SetHealth(_currentHealth);
            StartCoroutine(FlashRed());
            PlayerEvents.PlayerHit.Invoke();
            AudioManager.Instance.PlayPLayerHurtSound();

            //game over
            if (_invincible) return;
            if (!(_currentHealth <= 0)) return;
            StartCoroutine(Death());
            GameManager.Instance.playerSpeed = _playerMovement.GetSpeed();
            _playerMovement.IncreaseSpeed(0);
            _playerMovement.enabled = false;
            _playerInput.enabled = false;
            PlayerEvents.PlayerHit.Invoke();
            AudioManager.Instance.PlayPLayerHurtSound();
            GameManager.Instance.LoadDeathScreen();
        }
    }

    private IEnumerator FlashRed()
    {
        _playerRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _playerRenderer.color = Color.white;
    }

    private IEnumerator Death()
    {
        for (float i = 0; i < 0.5f; i += 0.1f)
        {
            _playerRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            _playerRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator FlashBlue()
    {
        _playerRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.2f);
        _playerRenderer.color = Color.white;
        yield return new WaitForSeconds(0.2f);
        _playerRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.2f);
        _playerRenderer.color = Color.white;
    }

    public void SetPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void IncreaseSpeed (float increaseMultiplier)
    {
        _speedMultiplier += increaseMultiplier;
        _playerMovement.IncreaseSpeed(_speedMultiplier);
        UIController.Instance.SetSpeedMultiplierText("+ " + Mathf.CeilToInt( (_speedMultiplier-1)*100 ) + " %");
        StartCoroutine(FlashBlue());
    }

    public void IncreaseHealth(float increaseMultiplier)
    {
        //TODO in future, for now not used
        _healthMultiplier += increaseMultiplier;
        _currentHealth = _currentHealth * _healthMultiplier;
        UIController.Instance.SetHealth(_currentHealth);
        StartCoroutine(FlashBlue());
    }

    public void RegenerateHealth(float value)
    {
        if (_currentHealth + value >= _maxHealth) _currentHealth = _maxHealth;
        else _currentHealth += value;
        UIController.Instance.SetHealth(_currentHealth);
        StartCoroutine(FlashBlue());
    }

    public void IncreaseStrength(float increaseMultiplier)
    {
        _strengthMultiplier += increaseMultiplier;
        UIController.Instance.SetStrengthMultiplierText("+ " + Mathf.CeilToInt( (_strengthMultiplier-1)*100 ) + " %");
        StartCoroutine(FlashBlue());
    }

    public void ModifyKilledReward(int delta)
    {
        _killedReward += delta;
        UIController.Instance.SetKilledRewardText("" + _killedReward);
    }

    public void ChangeWeapon(WeaponParent weapon, GameObject reward, IUsable usable)
    {
        ShowPlayerUI(true, "Press " + InputManager.Instance.GetKeyForAction(KeybindingActions.Interact) +
                           " to get new weapon.");
        _canGetWeapon = true;
        _weaponToGet = weapon;
        _weaponUsable = usable;
        _rewardToGet = reward;
    }

    public void PickUpAbility(Ability ability, GameObject reward, IUsable usable)
    {
        ShowPlayerUI(true, "Press " + InputManager.Instance.GetKeyForAction(KeybindingActions.Interact) +
                           " to get " + ability.GetText());
        _canGetAbility = true;
        _abilityToGet = ability;
        _abilityUsable = usable;
        _rewardToGet = reward;
    }

    public void PickUpPotion(GameObject reward)
    {
        // change to "Press keybindingAction.Interact.ToString() to get new weapon"
        ShowPlayerUI(true, "Press " + InputManager.Instance.GetKeyForAction(KeybindingActions.Interact) +
                           " to pick up healing potion.");
        _canGetPotion = true;
        _rewardToGet = reward;
    }

    public void DisableCanGetWeapon()
    {
        _canGetWeapon = false;
    }

    public void DisableCanGetPotion()
    {
        _canGetPotion = false;
    }

    public void DisableCanGetAbility()
    {
        _canGetAbility = false;
    }

    public void ShowPlayerUI(bool show, string text)
    {
        _playerUI.GetComponentInChildren<Text>().text = text;
        _playerUI.SetActive(show);
    }

    public float GetStrengthMultiplier()
    {
        return _strengthMultiplier;
    }

    public int GetKilledReward()
    {
        return _killedReward;
    }

    public bool checkSwordWeapon()
    {
        return _weaponParent.CompareTag("Sword");
    }
    public bool checkBowWeapon()
    {
        return _weaponParent.CompareTag("Bow");
    }
    public bool checkAxeWeapon()
    {
        return _weaponParent.CompareTag("Axe");
    }

    public float GetMaxHealth()
    {
        return _maxHealth;
    }

    public float GetCurrentHealth()
    {
        return _currentHealth;
    }

    public void ResetCurrentHealth()
    {
        _currentHealth = _maxHealth;
    }

    public void IncreaseMana(float value)
    {
        _currentMana += value;
        _currentMana = Math.Min(_currentMana, _maxMana);
        StartCoroutine(UIController.Instance.SetMana(_currentMana));
    }

    public void ManaToZero()
    {
        _currentMana = 0;
        StartCoroutine(UIController.Instance.SetMana(_currentMana));
    }

    public bool HasPotion()
    {
        return _hasPotion;
    }

    public void SetHasPotion(bool value)
    {
        _hasPotion = value;
    }

    public WeaponParent GetCurrentWeapon()
    {
        return _weaponParent;
    }

    public void ApplyEffect(StatusEffectData _data)
    {
        _statusEffect = _data;
        _statusEffectParticles = Instantiate(_data.particles, transform);
    }

    public void RemoveEffect()
    {
        Destroy(_statusEffectParticles);
        _statusEffect = null;
        _currentEffectTime = 0;
        _nextTickTime = 0;
    }

    private void HandleEffect()
    {
        _currentEffectTime += Time.deltaTime;

        if(_currentEffectTime >= _statusEffect.time) RemoveEffect();
        if (_statusEffect == null) return;
        if (_currentEffectTime > _nextTickTime)
        {
            _nextTickTime += _statusEffect.tickSpeed;
            _currentHealth -= _statusEffect.damage;
            UIController.Instance.SetHealth(_currentHealth);
        }
    }

    public PlayerMovement GetPlayerMovement()
    {
        return _playerMovement;
    }

    public PlayerInput GetPlayerInput()
    {
        return _playerInput;
    }

    public void ActivateAbility()
    {
        if (_ability && !_ability.IsActive()) //&& _currentMana==_maxMana
        {
            _ability.Activate();
            //_maxMana = 0;
        }
    }
}
