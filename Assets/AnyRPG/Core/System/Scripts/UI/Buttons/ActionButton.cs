using AnyRPG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnyRPG {
    public class ActionButton : NavigableElement, IClickable {

        // A reference to the useable on the actionbutton
        protected IUseable useable = null;

        // keep track of the last usable that was on this button in case an ability is re-learned
        protected IUseable savedUseable = null;

        [Header("Action Button")]

        [SerializeField]
        protected TextMeshProUGUI stackSizeText = null;

        [SerializeField]
        protected TextMeshProUGUI keyBindText = null;

        [SerializeField]
        protected Image backgroundImage = null;

        [SerializeField]
        protected Image icon = null;

        [SerializeField]
        protected Image coolDownIcon = null;

        [SerializeField]
        protected Image backGroundImage = null;

        [SerializeField]
        protected Image rangeIndicator = null;

        protected int count = 0;

        protected int actionButtonIndex = 0;
        protected bool gamepadButton = false;

        protected Coroutine monitorCoroutine = null;

        protected CloseableWindowContents windowPanel = null;

        // game manager references
        protected UIManager uIManager = null;
        protected SystemEventManager systemEventManager = null;
        protected PlayerManager playerManager = null;
        protected HandScript handScript = null;
        protected ActionBarManager actionBarManager = null;
        //InventoryManager inventoryManager = null;

        /// <summary>
        /// A reference to the actual button that this button uses
        /// </summary>
        public Button Button { get; private set; }

        public Image Icon { get => icon; set => icon = value; }
        public int Count { get => count; }
        public TextMeshProUGUI StackSizeText { get => stackSizeText; }
        public TextMeshProUGUI KeyBindText { get => keyBindText; }
        public IUseable SavedUseable { get => savedUseable; set => savedUseable = value; }
        public IUseable Useable {
            get {
                return useable;
            }
            set {
                if (value == null) {
                    useable = value;
                    return;
                }
                useable = value.GetFactoryUseable();
            }
        }
        public Image CoolDownIcon { get => coolDownIcon; set => coolDownIcon = value; }
        public Coroutine MonitorCoroutine { get => monitorCoroutine; set => monitorCoroutine = value; }
        public Image BackgroundImage { get => backgroundImage; set => backgroundImage = value; }
        public Image RangeIndicator { get => rangeIndicator; set => rangeIndicator = value; }

        public override void Configure(SystemGameManager systemGameManager) {
            if (configureCount > 0) {
                return;
            }
            base.Configure(systemGameManager);

            uIManager = systemGameManager.UIManager;
            systemEventManager = systemGameManager.SystemEventManager;
            playerManager = systemGameManager.PlayerManager;
            handScript = uIManager.HandScript;
            actionBarManager = uIManager.ActionBarManager;
            //inventoryManager = systemGameManager.InventoryManager;

            Useable = null;
            Button = GetComponent<Button>();
            Button.onClick.AddListener(OnClickFromButton);
            if (backGroundImage == null) {
                backGroundImage = GetComponent<Image>();
            }
            DisableCoolDownIcon();

            systemEventManager.OnItemCountChanged += UpdateItemCount;
            rangeIndicator.color = hiddenColor;
        }

        
        public void SetIndex(int index) {
            actionButtonIndex = index;
            // for now, we only set index on gamepad buttons, so this call can tell the button it's a gamepad button
            gamepadButton = true;
        }

        public void SetPanel(CloseableWindowContents windowPanel) {
            this.windowPanel = windowPanel;
        }
        

        public void SetBackGroundColor(Color color) {
            if (backGroundImage != null) {
                backGroundImage.color = color;
            }
        }

        public void OnClickFromButton() {
            OnClick();
        }

        public void OnClick(bool fromKeyBind = false) {
            // this may seem like duplicate with the next method, but right now it is used to simulate click events when keypresses happen

            if (!fromKeyBind) {
                // if we did come from a keybind, we don't want to ignore left shift
                if (Input.GetKey(KeyCode.LeftShift)) {
                    return;
                }
                if (handScript.Moveable != null) {
                    // if we have something in the handscript we are trying to drop an item, not use one
                    return;
                }
            }

            if (Useable != null) {
                Useable.ActionButtonUse();
            }
        }

        public override void OnPointerClick(PointerEventData eventData) {
            base.OnPointerClick(eventData);
            if (playerManager.ActiveUnitController != null) {
                if (playerManager.ActiveUnitController.ControlLocked == true) {
                    return;
                }
            }

            // left click
            if (eventData.button == PointerEventData.InputButton.Left) {

                if (Input.GetKey(KeyCode.LeftShift)) {
                    // attempt to pick up - the only valid option when shift is held down
                    if (Useable != null && actionBarManager.FromButton == null && handScript.Moveable == null) {
                        // left shift down, pick up a useable
                        //Debug.Log("ActionButton: OnPointerClick(): shift clicked and useable is not null. picking up");
                        handScript.TakeMoveable(Useable as IMoveable);
                        actionBarManager.FromButton = this;
                    }
                } else {
                    // attempt to put down
                    if (handScript.Moveable != null && handScript.Moveable is IUseable) {
                        if (actionBarManager.FromButton != null) {
                            //Debug.Log("ActionButton: OnPointerClick(): FROMBUTTON IS NOT NULL, SWAPPING ACTIONBAR ITEMS");
                            // this came from another action button slot.  now decide to swap (if we are not empty), or remove from original (if we are empty)
                            if (Useable != null) {
                                actionBarManager.FromButton.ClearUseable();
                                actionBarManager.FromButton.SetUseable(Useable);
                            } else {
                                actionBarManager.FromButton.ClearUseable();
                            }
                        }
                        // no matter whether we sent our useable over or not, we can now clear our useable and set whatever is in the handscript
                        ClearUseable();
                        SetUseable(handScript.Moveable as IUseable);

                        handScript.Drop();
                    }
                }
            }
        }

        public void HandleDropCombat() {
            UpdateVisual();
        }

        public void HandleEnterCombat(Interactable interactable) {
            UpdateVisual();
        }

        /// <summary>
        /// Sets the useable on the actionbutton
        /// </summary>
        /// <param name="useable"></param>
        public void SetUseable(IUseable useable, bool monitor = true) {
            //Debug.Log(gameObject.name + ".ActionButton.SetUsable(" + (useable == null ? "null" : useable.DisplayName) + ")");
            playerManager.MyCharacter.CharacterAbilityManager.OnAttemptPerformAbility -= OnAttemptUseableUse;
            playerManager.MyCharacter.CharacterAbilityManager.OnPerformAbility -= OnUseableUse;
            playerManager.MyCharacter.CharacterAbilityManager.OnBeginAbilityCoolDown -= HandleBeginAbilityCooldown;

            UnsubscribeFromCombatEvents();

            DisableCoolDownIcon();

            useable.AssignToActionButton(this);
            Useable = useable;

            playerManager.MyCharacter.CharacterAbilityManager.OnAttemptPerformAbility += OnAttemptUseableUse;
            playerManager.MyCharacter.CharacterAbilityManager.OnPerformAbility += OnUseableUse;
            playerManager.MyCharacter.CharacterAbilityManager.OnBeginAbilityCoolDown += HandleBeginAbilityCooldown;

            SubscribeToCombatEvents();

            // there may be a global cooldown in progress.  if not, this call will still update the visual
            if (monitor == true) {
                ChooseMonitorCoroutine();
            }

            // there was the assumption that these were only being called when a player clicked to add an ability
            if (UIManager.MouseInRect(Icon.rectTransform)) {
                uIManager.ShowToolTip(transform.position, useable as IDescribable);
            }

            //if (gamepadButton == true) {
                //rangeIndicator.color = Color.white;
            //}
        }

        public void SubscribeToCombatEvents() {
            if (Useable != null && Useable is BaseAbility && (Useable as BaseAbility).RequireOutOfCombat == true) {
                playerManager.MyCharacter.CharacterCombat.OnEnterCombat += HandleEnterCombat;
                playerManager.MyCharacter.CharacterCombat.OnDropCombat += HandleDropCombat;
            }
        }

        public void OnAttemptUseableUse(BaseAbility ability) {
            //Debug.Log("ActionButton.OnUseableUse(" + ability.DisplayName + ")");
            ChooseMonitorCoroutine();
        }

        public void HandleBeginAbilityCooldown() {
            //Debug.Log("ActionButton.OnUseableUse(" + ability.DisplayName + ")");
            ChooseMonitorCoroutine();
        }

        private void ChooseMonitorCoroutine() {
            // if this action button is empty, there is nothing to monitor
            if (useable == null) {
                return;
            }
            if (monitorCoroutine == null) {
                monitorCoroutine = useable.ChooseMonitorCoroutine(this);
            }
            if (monitorCoroutine == null) {
                UpdateVisual();
            }
        }

        public void OnUseableUse(BaseAbility ability) {
            //Debug.Log("ActionButton.OnUseableUse(" + ability.DisplayName + ")");
            ChooseMonitorCoroutine();
        }

        public IEnumerator MonitorAutoAttack(BaseAbility ability) {
            //Debug.Log("ActionButton.MonitorautoAttack(" + ability.DisplayName + ")");
            yield return null;

            while (Useable != null
                && playerManager.MyCharacter.CharacterCombat.GetInCombat() == true
                && playerManager.MyCharacter.CharacterCombat.AutoAttackActive == true) {
                UpdateVisual();
                yield return new WaitForSeconds(0.5f);
            }
            //Debug.Log("ActionButton.MonitorAbility(" + ability.DisplayName + "): Done Monitoring");
            if (Useable != null) {
                // could switch buttons while an ability is on cooldown
                UpdateVisual();
            }
            //autoAttackCoRoutine = null;
            monitorCoroutine = null;
        }

        public IEnumerator MonitorAbility(BaseAbility ability) {
            //Debug.Log("ActionButton.MonitorAbility(" + ability.DisplayName + ")");
            while (Useable != null
                && (playerManager.MyCharacter.CharacterAbilityManager.RemainingGlobalCoolDown > 0f
                || playerManager.MyCharacter.CharacterAbilityManager.MyAbilityCoolDownDictionary.ContainsKey(ability.DisplayName))) {
                UpdateVisual();
                yield return null;
            }
            //Debug.Log("ActionButton.MonitorAbility(" + ability.DisplayName + "): Done Monitoring; Useable: " + (Useable == null ? "null" : Useable.DisplayName));
            if (Useable != null) {
                // could switch buttons while an ability is on cooldown
                UpdateVisual();
            }
            //abilityCoRoutine = null;
            monitorCoroutine = null;
        }

        public void DisableCoolDownIcon() {
            //Debug.Log("ActionButton.DisableCoolDownIcon() useable: " + (useable == null ? "null" : useable.DisplayName));
            // testing
            // this was preventing cooldown icons from being reset on logout
            //if (coolDownIcon.isActiveAndEnabled != false) {
            coolDownIcon.sprite = null;
            coolDownIcon.color = new Color32(0, 0, 0, 0);
            coolDownIcon.enabled = false;
            //}
        }

        /// <summary>
        /// Updates the visual representation of the actionbutton
        /// </summary>
        public void UpdateVisual(bool removeStaleActions = false) {
            //Debug.Log(gameObject.name + GetInstanceID() + ".ActionButton.UpdateVisual() useable: " + (useable == null ? "null" : useable.DisplayName));
            if (playerManager == null || playerManager.MyCharacter == null) {
                return;
            }
            // attempt to remove unlearned spells from the bars
            if (removeStaleActions) {
                //Debug.Log("ActionButton.UpdateVisual(): removeStaleActions = true");
                if (Useable != null && Useable.IsUseableStale(this)) {
                    if (!playerManager.MyCharacter.CharacterAbilityManager.HasAbility(Useable as BaseAbility)) {
                        savedUseable = Useable;
                        Useable = null;
                    }
                }
            }

            if (Useable == null) {
                //Debug.Log("ActionButton.UpdateVisual(): useable is null. clearing stack count and setting icon to empty");
                
                // clear stack count
                uIManager.ClearStackCount(this);

                // clear icon
                Icon.sprite = null;
                Icon.color = icon.color = new Color32(0, 0, 0, 0);
                
                // clear background image
                backgroundImage.color = new Color32(0, 0, 0, 0);
                backgroundImage.sprite = null;

                // clear cooldown icon
                DisableCoolDownIcon();

                return;
            }

            if (Icon.sprite != Useable.Icon) {
                Icon.sprite = Useable.Icon;
            }
            if (Icon.color != Color.white) {
                Icon.color = Color.white;
            }

            //Debug.Log("ActionButton.UpdateVisual(): about to get useable count");
            Useable.UpdateChargeCount(this);
            Useable.UpdateActionButtonVisual(this);

            if (UIManager.MouseInRect(Icon.rectTransform)) {
                ProcessOnPointerEnter();
            }
        }

        public void EnableFullCoolDownIcon() {
            //Debug.Log("ActionButton.EnableFullCoolDownIcon(): useable: " + (useable == null ? "null" : useable.DisplayName));
            if (coolDownIcon.isActiveAndEnabled == false) {
                coolDownIcon.enabled = true;
            }
            if (coolDownIcon.sprite != Icon.sprite) {
                coolDownIcon.sprite = Icon.sprite;
            }
            coolDownIcon.color = new Color32(0, 0, 0, 150);
            coolDownIcon.fillMethod = Image.FillMethod.Radial360;
            //coolDownIcon.fillOrigin = Image.Origin360.Top;
            coolDownIcon.fillClockwise = false;
            //}
            float fillAmount = 1f;
            if (coolDownIcon.fillAmount != fillAmount) {
                coolDownIcon.fillAmount = fillAmount;
            }
        }

        public void UpdateItemCount(Item item) {

            if (item is IUseable) {
                UpdateVisual();
            }
        }

        public override void OnPointerEnter(PointerEventData eventData) {
            base.OnPointerEnter(eventData);
            //Debug.Log(gameObject + ".ActionButton.OnPointerEnter()");

            ProcessOnPointerEnter();
        }

        public void ProcessOnPointerEnter() {
            IDescribable tmp = null;

            if (Useable != null && Useable is IDescribable) {
                tmp = (IDescribable)Useable;
            }
            if (tmp != null) {
                uIManager.ShowToolTip(transform.position, tmp);
            }
        }

        public override void OnPointerExit(PointerEventData eventData) {
            base.OnPointerExit(eventData);
            uIManager.HideToolTip();
        }

        public void UnsubscribeFromCombatEvents() {
            if (Useable != null && Useable is BaseAbility && (Useable as BaseAbility).RequireOutOfCombat == true) {
                playerManager.MyCharacter.CharacterCombat.OnEnterCombat -= HandleEnterCombat;
                playerManager.MyCharacter.CharacterCombat.OnDropCombat -= HandleDropCombat;
            }
        }

        public void ClearUseable() {
            //Debug.Log("ActionButton.ClearUseable()");

            UnsubscribeFromCombatEvents();
            if (Useable != null) {
                savedUseable = Useable;
            }
            Useable = null;
            DisableCoolDownIcon();
            UpdateVisual();

            rangeIndicator.color = hiddenColor;
        }

        public override void Select() {
            base.Select();
            if (useable != null) {
                owner.SetControllerHints("Move", "Clear", "", "");
                if (windowPanel != null) {
                    uIManager.ShowGamepadTooltip(windowPanel.RectTransform, transform, useable as IDescribable, "");
                }
            } else {
                owner.HideControllerHints();
                uIManager.HideToolTip();
            }
        }

        public override void DeSelect() {
            base.DeSelect();
            owner.HideControllerHints();
            uIManager.HideToolTip();
        }

        public override void JoystickButton2() {
            base.JoystickButton2();
            if (useable == null) {
                return;
            }
            actionBarManager.ClearUseableByIndex(actionButtonIndex);
            owner.HideControllerHints();
        }

        public override void Accept() {
            base.Accept();
            if (useable == null) {
                return;
            }
            actionBarManager.StartUseableAssignment(useable, actionButtonIndex);

            // ensure the assignment window is set to the same navigation controller and element index so the move starts in the same spot on the screen
            (uIManager.assignToActionBarsWindow.CloseableWindowContents as AssignToActionBarsUI).SetNavigationControllerByIndex(windowPanel.GetNavigationControllerIndex());
            (uIManager.assignToActionBarsWindow.CloseableWindowContents as AssignToActionBarsUI).CurrentNavigationController.SetCurrentIndex(windowPanel.CurrentNavigationController.CurrentIndex);

            uIManager.assignToActionBarsWindow.OpenWindow();
        }
    }

}