﻿using RTSEngine.Audio;
using RTSEngine.Entities;
using RTSEngine.Game;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Selection.EntityGroup
{
    public class EntityGroupSelection : MonoBehaviour
    {
        [System.Serializable]
        public struct IEntityGroupSelectionSlot
        {
            [Tooltip("Entity types allowed in the slot. Leave empty for all types.")]
            public EntityType[] allowedEntityType;

            [Tooltip("Key used to assign a group of selected entities to the slot.")]
            public KeyCode key;

            [HideInInspector]
            public List<IEntity> units;
        }
        [SerializeField, Tooltip("Each slot allows a group of selected entities can be stored so they can be selected all together again using one key. The amount of available slots to the player is the size of this array.")]
        private IEntityGroupSelectionSlot[] slots = new IEntityGroupSelectionSlot[0]; 

        //when this key is pressed along with one of the group selection slots' key then the group selection slot will be set.
        [SerializeField]
        private KeyCode assignGroupKey = KeyCode.LeftShift;

        [SerializeField]
        private bool showUIMessages = true; //when enabled, each group assign/selection will show a UI message to the player

        [SerializeField, Tooltip("Audio clip to play when a selection group is assigned.")]
        private AudioClipFetcher assignGroupAudio = new AudioClipFetcher(); //played when a selection group slot is assigned
        [SerializeField, Tooltip("Audio clip to play when a selection group is activated (selected).")]
        private AudioClipFetcher selectGroupAudio = new AudioClipFetcher(); //played when a selection group slot is activated
        [SerializeField, Tooltip("Audio clip to play when a selection group is activated but it is empty.")]
        private AudioClipFetcher groupEmptyAudio = new AudioClipFetcher(); //played when the player attempts to activate the selection of a group slot but it happens to be empty

        protected IGameAudioManager audioMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>(); 
        }

        private void Update()
        {
            foreach(IEntityGroupSelectionSlot slot in slots) //go through all the group selection slots
            {
                if(Input.GetKeyDown(slot.key)) //if the player presses both the slot specific key
                {
                    if(Input.GetKey(assignGroupKey)) //if the player presses the group slot assign key at the same time -> assign group
                    {
                        List<IUnit> selectedUnits = selectionMgr.GetEntitiesList(EntityType.unit, true, true).Cast<IUnit>().ToList(); //get selected units from player faction

                        if(selectedUnits.Count > 0) //make sure that there's at least one unit selected
                        {
                            //assign this new group to them
                            slot.units.Clear();
                            slot.units.AddRange(selectedUnits);

                            //play audio:
                            audioMgr.PlaySFX(assignGroupAudio.Fetch(), false);

                            //inform player about assigning a new selection group:
                            /*if (showUIMessages)
                                PlayerErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupSet, null);*/
                        }
                    }
                    else //the assign group key hasn't been assigned -> select units in this slot if there are any
                    {
                        bool found = false; //determines whether there are actually units in the list
                        //it might be that the previously assigned units to this slot are all dead and therefore all slots are referencing null

                        int i = 0; //we'll be also clearing empty slots
                        while(i < slot.units.Count)
                        {
                            if (slot.units[i] == null) //if this element is invalid
                                slot.units.RemoveAt(i); //remove it
                            else
                            {
                                if (found == false) //first time encountering a valid
                                    selectionMgr.RemoveAll(); //deselect the currently selected units.

                                selectionMgr.Add(slot.units[i], SelectionType.multiple); //add unit to selection
                                found = true;
                            }

                            i++;
                        }

                        if(found == true) //making sure that there are valid units in the list that have been selected:
                        {
                            //play audio:
                            audioMgr.PlaySFX(selectGroupAudio.Fetch(), false);

                            //inform player about selecting:
                            /*if (showUIMessages)
                                PlayerErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupSelected, null);*/
                        }
                        else //the list is either empty or all elements are invalid
                        {
                            //play audio:
                            audioMgr.PlaySFX(groupEmptyAudio.Fetch(), false);

                            //inform player about the empty group:
                            /*if (showUIMessages)
                                PlayerErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupEmpty, null);*/
                        }
                    }
                }
            }
        }
    }
}
