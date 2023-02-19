using AnyRPG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AnyRPG {
    [System.Serializable]
    public class LearnAbilityObjective : QuestObjective {

        [SerializeField]
        [ResourceSelector(resourceType = typeof(BaseAbility))]
        protected string abilityName = null;

        public override string ObjectiveName { get => abilityName; }

        public override Type ObjectiveType {
            get {
                return typeof(LearnAbilityObjective);
            }
        }

        private BaseAbilityProperties baseAbility;

        // for learning
        public void UpdateCompletionCount() {
            //Debug.Log("AbilityObjective.UpdateCompletionCount(" + (baseAbility == null ? "null" : baseAbility.DisplayName) + ")");
            bool completeBefore = IsComplete;
            if (completeBefore) {
                return;
            }
            CurrentAmount++;
            quest.CheckCompletion();
            if (CurrentAmount <= Amount && !quest.IsAchievement && CurrentAmount != 0) {
                messageFeedManager.WriteMessage(string.Format("{0}: {1}/{2}", DisplayName, Mathf.Clamp(CurrentAmount, 0, Amount), Amount));
            }
            if (completeBefore == false && IsComplete && !quest.IsAchievement) {
                messageFeedManager.WriteMessage(string.Format("Learn {0} {1}: Objective Complete", CurrentAmount, DisplayName));
            }
        }

        public override void UpdateCompletionCount(bool printMessages = true) {

            base.UpdateCompletionCount(printMessages);
            bool completeBefore = IsComplete;
            if (completeBefore) {
                return;
            }
            if (playerManager.MyCharacter.CharacterAbilityManager.HasAbility(baseAbility)) {
                CurrentAmount++;
                quest.CheckCompletion(true, printMessages);
                if (CurrentAmount <= Amount && !quest.IsAchievement && printMessages == true) {
                    messageFeedManager.WriteMessage(string.Format("{0}: {1}/{2}", baseAbility.DisplayName, CurrentAmount, Amount));
                }
                if (completeBefore == false && IsComplete && !quest.IsAchievement && printMessages == true) {
                    messageFeedManager.WriteMessage(string.Format("Learn {0} {1}: Objective Complete", CurrentAmount, baseAbility.DisplayName));
                }
            }
        }

        public override void OnAcceptQuest(Quest quest, bool printMessages = true) {
            base.OnAcceptQuest(quest, printMessages);
            baseAbility.OnAbilityLearn += UpdateCompletionCount;
            UpdateCompletionCount(printMessages);
        }

        public override void OnAbandonQuest() {
            base.OnAbandonQuest();
            baseAbility.OnAbilityLearn -= UpdateCompletionCount;
        }

        public override string GetUnformattedStatus() {
            return "Learn " + DisplayName + ": " + Mathf.Clamp(CurrentAmount, 0, Amount) + "/" + Amount;
        }

        public override void SetupScriptableObjects(SystemGameManager systemGameManager, Quest quest) {
            base.SetupScriptableObjects(systemGameManager, quest);
            
            if (abilityName != null && abilityName != string.Empty) {
                BaseAbility tmpAbility = systemDataFactory.GetResource<BaseAbility>(abilityName);
                if (tmpAbility != null) {
                    baseAbility = tmpAbility.AbilityProperties;
                } else {
                    Debug.LogError("AbilityObjective.SetupScriptableObjects(): Could not find ability : " + abilityName + " while inititalizing an ability objective for " + quest.DisplayName + ".  CHECK INSPECTOR");
                }
            }
        }

    }

}