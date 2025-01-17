using AnyRPG;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AnyRPG {
    public class PatrolController : ConfiguredClass {

        // references
        private UnitController unitController;

        private List<PatrolProperties> patrolPropsList = new List<PatrolProperties>();
        private Dictionary<PatrolProperties, PatrolSaveState> patrolSaveStates = new Dictionary<PatrolProperties, PatrolSaveState>();

        private PatrolProperties currentPatrolProps = null;

        public PatrolProperties CurrentPatrol { get => currentPatrolProps; }
        public PatrolProperties CurrentPatrolProps { get => currentPatrolProps; }
        public UnitController UnitController { get => unitController; }
        public PatrolSaveState CurrentPatrolSaveState {
            get {
                if (CurrentPatrol != null) {
                    return patrolSaveStates[CurrentPatrol];
                }
                return null;
            }
        }

        public PatrolController(UnitController unitController, SystemGameManager systemGameManager) {
            this.unitController = unitController;
            Configure(systemGameManager);
        }

        // this should be run after the unit profile is set
        public void Init() {
            //Debug.Log("PatrolController.Init()");

            SetupScriptableObjects();

            FindAutomaticPatrol();
        }

        public void BeginPatrolByIndex(int patrolIndex) {
            if (patrolIndex < 0 || patrolIndex >= patrolPropsList.Count) {
                //Debug.Log("PatrolController.BeginPatrolByIndex(" + patrolIndex + "): invalid index");
                return;
            }
            BeginPatrol(patrolPropsList[patrolIndex]);
        }

        public void BeginPatrol(string patrolName) {
            //Debug.Log(unitController.gameObject.name + ".PatrolController.BeginPatrol(" + (patrolName != null ? patrolName : "null" ) + ")");
            PatrolProfile tmpPatrolProfile = systemDataFactory.GetResource<PatrolProfile>(patrolName);
            if (tmpPatrolProfile != null) {
                //if (patrolSaveStates.ContainsKey(tmpPatrolProfile.PatrolProperties) == false) {
                    AddPatrolState(tmpPatrolProfile.PatrolProperties);
                //}
                BeginPatrol(tmpPatrolProfile.PatrolProperties);
                return;
            } else {
                Debug.LogError(unitController.gameObject.name + ".PatrolController.BeginPatrol() could not find patrol: " + (patrolName != null ? patrolName : "null") + ")");
            }
        }

        public void BeginPatrol(PatrolProperties patrolProps) {
            //Debug.Log(unitController.gameObject.name + ".PatrolController.BeginPatrol(" + (patrolProps == null ? "null" : "valid patrolProps") + ")");

            // testing - monitor if putting this here instead of in AddPatrolState() breaks anything
            if (patrolSaveStates.ContainsKey(patrolProps)) {
                patrolSaveStates[patrolProps] = new PatrolSaveState(this, patrolProps);
            } else {
                patrolSaveStates.Add(patrolProps, new PatrolSaveState(this, patrolProps));
            }

            SetCurrentPatrol(patrolProps);
            unitController.ChangeState(new PatrolState());
            return;
        }

        private void AddPatrolState(PatrolProperties patrolProps) {
            if (patrolPropsList.Contains(patrolProps) == false) {
                patrolPropsList.Add(patrolProps);
            }
            /*
            if (patrolSaveStates.ContainsKey(patrolProps)) {
                patrolSaveStates[patrolProps] = new PatrolSaveState(this, patrolProps);
            } else {
                patrolSaveStates.Add(patrolProps, new PatrolSaveState(this, patrolProps));
            }
            */
        }


        public void SetCurrentPatrol(PatrolProperties newPatrolProps) {
            currentPatrolProps = newPatrolProps;
        }

        private void FindAutomaticPatrol() {
            //Debug.Log(unitController.gameObject.name + ".patrolController.FindAutomaticPatrol()");
            if (unitController.UnitControllerMode != UnitControllerMode.AI) {
                return;
            }

            foreach (PatrolProperties patrolProps in patrolPropsList) {
                //Debug.Log(unitController.gameObject.name + ".patrolController.FindAutomaticPatrol(): found patrol profile: " + patrolProfile.DisplayName);
                if (patrolProps.AutoStart == true) {
                    //Debug.Log(unitController.gameObject.name + ".patrolController.FindAutomaticPatrol(): found autostart profile");
                    BeginPatrol(patrolProps);
                    break;
                }
            }
        }

        private void SetupScriptableObjects() {
            //Debug.Log(unitController.gameObject.name + ".patrolController.SetupScriptableObjects()");

            // local patrols
            if (unitController?.PatrolNames != null) {
                foreach (string patrolName in unitController.PatrolNames) {
                    if (patrolName != null && patrolName != string.Empty) {
                        PatrolProfile _tmpPatrolProfile = systemDataFactory.GetResource<PatrolProfile>(patrolName);
                        if (_tmpPatrolProfile != null) {
                            AddPatrolState(_tmpPatrolProfile.PatrolProperties);
                        } else {
                            Debug.LogError("PatrolController.SetupScriptableObjects: could not find patrol name: " + patrolName);
                        }
                    }
                }
            }

            // patrols from unit profile
            if (unitController?.UnitProfile?.PatrolNames != null) {
                foreach (string patrolName in unitController.UnitProfile.PatrolNames) {
                    if (patrolName != null && patrolName != string.Empty) {
                        PatrolProfile _tmpPatrolProfile = systemDataFactory.GetResource<PatrolProfile>(patrolName);
                        if (_tmpPatrolProfile != null) {
                            AddPatrolState(_tmpPatrolProfile.PatrolProperties);
                        } else {
                            Debug.LogError("PatrolController.SetupScriptableObjects: could not find patrol name: " + patrolName);
                        }
                    }
                }
            }

            if (unitController?.UnitProfile?.UseInlinePatrol == true) {
                AddPatrolState(unitController.UnitProfile.PatrolConfig);
            }


        }

    }

}