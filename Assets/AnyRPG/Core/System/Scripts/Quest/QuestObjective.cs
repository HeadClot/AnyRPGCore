using AnyRPG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyRPG {
    [System.Serializable]
    public class QuestObjective : ConfiguredClass {
        [SerializeField]
        private int amount = 1;

        protected QuestBase questBase;

        [Tooltip("Set this if you want to override the name shown in the quest log objective to be something other than the type")]
        [SerializeField]
        private string overrideDisplayName = string.Empty;

        // game manager references
        protected SaveManager saveManager = null;
        protected MessageFeedManager messageFeedManager = null;
        protected SystemEventManager systemEventManager = null;
        protected PlayerManager playerManager = null;
        protected LevelManager levelManager = null;

        public int Amount {
            get {
                return (int)Mathf.Clamp(amount, 1, Mathf.Infinity);
            }
            set {
                amount = value;
            }
        }

        public virtual Type ObjectiveType {
            get {
                return typeof(QuestObjective);
            }
        }

        public int CurrentAmount {
            get {
                return saveManager.GetQuestObjectiveSaveData(questBase.DisplayName, ObjectiveType.Name, ObjectiveName).Amount;
                //return false;
            }
            set {
                QuestObjectiveSaveData saveData = saveManager.GetQuestObjectiveSaveData(questBase.DisplayName, ObjectiveType.Name, ObjectiveName);
                saveData.Amount = value;
                saveManager.QuestObjectiveSaveDataDictionary[questBase.DisplayName][ObjectiveType.Name][ObjectiveName] = saveData;
            }
        }

        public virtual string ObjectiveName { get => string.Empty; }

        public virtual bool IsComplete {
            get {
                //Debug.Log("checking if quest objective iscomplete, current: " + MyCurrentAmount.ToString() + "; needed: " + amount.ToString());
                return CurrentAmount >= Amount;
            }
        }

        public QuestBase QuestBase { get => questBase; set => questBase = value; }
        public string OverrideDisplayName { get => overrideDisplayName; set => overrideDisplayName = value; }
        public string DisplayName {
            get {
                if (overrideDisplayName != string.Empty) {
                    return overrideDisplayName;
                }
                return ObjectiveName;
            }
            set => overrideDisplayName = value;
        }

        public virtual void UpdateCompletionCount(bool printMessages = true) {
            //Debug.Log("QuestObjective.UpdateCompletionCount()");
        }

        public virtual void OnAcceptQuest(QuestBase questBase, bool printMessages = true) {
            this.questBase = questBase;
        }

        private void SetQuest(QuestBase questBase) {
            this.questBase = questBase;
        }

        public virtual void OnAbandonQuest() {
            // overwrite me
        }

        public virtual void HandleQuestStatusUpdated() {
            UpdateCompletionCount();
        }

        public virtual string GetUnformattedStatus() {
            return DisplayName + ": " + Mathf.Clamp(CurrentAmount, 0, Amount) + "/" + Amount;
        }

        public virtual void SetupScriptableObjects(SystemGameManager systemGameManager, QuestBase quest) {
            Configure(systemGameManager);
            SetQuest(quest);
        }

        public override void SetGameManagerReferences() {
            base.SetGameManagerReferences();
            saveManager = systemGameManager.SaveManager;
            messageFeedManager = systemGameManager.UIManager.MessageFeedManager;
            systemEventManager = systemGameManager.SystemEventManager;
            playerManager = systemGameManager.PlayerManager;
            levelManager = systemGameManager.LevelManager;
        }
    }


}