using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using GameNetcodeStuff;
using PathfindingLib.Jobs;
using PathfindingLib.Utilities;
using Scopophobia;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Zeekerss.Core.Singletons;

namespace ShyGuy.AI
{
    public class ShyGuyAI : EnemyAI
    {
        private Transform localPlayerCamera;

        public Vector3 mainEntrancePosition;

        public Collider mainCollider;

        public VehicleController CompanyCruiser;
        public bool pryingOpenDoor;
        public bool pathingToTeleport;
        public Vector3 closestTeleportPosition;
        public HangarShipDoor shipDoor;

        private float pryingDoorAnimTime;

        public float pryOpenDoorAnimLength;

        public AudioClip breakAndEnter;

        public AudioClip shipAlarm;

        public AudioSource breakDownDoorAudio;

        public AudioSource farAudio;
        public string typeName = "ShyGuyAI";
        public AudioSource footstepSource;

        public AudioClip screamSFX;

        public AudioClip panicSFX;

        public AudioClip crySFX;

        public AudioClip crySittingSFX;

        public AudioClip killPlayerSFX;

        [Header("Containment Breach Sounds")]
        public AudioClip screamSFX_CB;

        public AudioClip panicSFX_CB;

        public AudioClip crySFX_CB;

        public AudioClip killPlayerSFX_CB;

        [Header("Alpha Containment Breach Sounds")]
        public AudioClip screamSFX_ACB;

        public AudioClip panicSFX_ACB;

        public AudioClip crySFX_ACB;

        public AudioClip killPlayerSFX_ACB;

        [Header("Secret Laboratory Sounds")]
        public AudioClip screamSFX_SL;

        public AudioClip panicSFX_SL;

        public AudioClip crySFX_SL;

        public AudioClip killPlayerSFX_SL;

        public Material bloodyMaterial;

        public AISearchRoutine roamMap;
        public static EntranceTeleport[] entranceTeleports;
        public static List<EntranceTeleport> outsideTeleports = [];
        public static List<EntranceTeleport> insideTeleports = [];
        public Transform shyGuyFace;

        public EntranceTeleport closestTeleport;

        private Vector3 spawnPosition;

        private Vector3 previousPosition;

        private int previousState = -1;

        private float roamWaitTime = 40f;

        private bool roamShouldSit;

        private bool sitting;

        private float lastRunSpeed;

        private float seeFaceTime;

        private float triggerTime;

        private float triggerDuration = 66.4f;

        private float timeToTrigger = 0.5f;

        private float lastInterval;

        private bool inKillAnimation;

        private bool isInElevatorStartRoom;

        private float timeAtLastUsingEntrance;

        public static MineshaftElevatorController elevatorScript;
        public List<PlayerControllerB> SCP096Targets = new List<PlayerControllerB>();

        public override void Start()
        {
            base.Start();
            if(CompanyCruiser == null) CompanyCruiser = UnityEngine.Object.FindObjectOfType<VehicleController>();
            if(shipDoor == null) shipDoor = UnityEngine.Object.FindObjectOfType<HangarShipDoor>();
            if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
            if(elevatorScript == null) elevatorScript = UnityEngine.Object.FindObjectOfType<MineshaftElevatorController>();//Lets see if the Elevator Controls exist, I hope they do, we're on the mineshaft
            triggerDuration = Config.triggerTime;
            lastInterval = Time.realtimeSinceStartup;
            Transform leftEye = null;
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(transform);
            while (queue.Count > 0)
            {
                Transform c = queue.Dequeue();
                if (c.name == "lefteye")
                {
                    leftEye = c;
                    break;
                }
                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }
            Transform rightEye = null;
            queue = new Queue<Transform>();
            queue.Enqueue(transform);
            while (queue.Count > 0)
            {
                Transform c = queue.Dequeue();
                if (c.name == "righteye")
                {
                    rightEye = c;
                    break;
                }
                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }
            if (!Config.hasGlowingEyes && leftEye != null && rightEye != null)
            {
                leftEye.gameObject.SetActive(value: false);
                rightEye.gameObject.SetActive(value: false);
            }
            if (Config.bloodyTexture && bloodyMaterial != null)
            {
                Transform model = transform.Find("SCP096Model");
                if (model != null)
                {
                    Transform modelMesh = model.Find("tsg_placeholder");
                    if (modelMesh != null)
                    {
                        SkinnedMeshRenderer skinnedModel = modelMesh.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedModel != null)
                        {
                            skinnedModel.material = bloodyMaterial;
                        }
                    }
                }
            }
            switch (Config.soundPack)
            {
                case "SCPCB":
                    screamSFX = screamSFX_CB;
                    crySFX = crySFX_CB;
                    crySittingSFX = crySFX_CB;
                    panicSFX = panicSFX_CB;
                    killPlayerSFX = killPlayerSFX_CB;
                    break;
                case "SCPCBOld":
                    screamSFX = screamSFX_ACB;
                    crySFX = crySFX_ACB;
                    crySittingSFX = crySFX_ACB;
                    panicSFX = panicSFX_ACB;
                    killPlayerSFX = killPlayerSFX_ACB;
                    break;
                case "SecretLab":
                    screamSFX = screamSFX_SL;
                    crySFX = crySFX_SL;
                    crySittingSFX = crySFX_SL;
                    panicSFX = panicSFX_SL;
                    killPlayerSFX = killPlayerSFX_SL;
                    break;
            }
            localPlayerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform;
            spawnPosition = transform.position;
            isOutside = transform.position.y > -80f;
            mainEntrancePosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isOutside));
            path1 = new NavMeshPath();
            //the Following Code is adapted on code from StarlancerAIFix. 
            outsideTeleports.Clear();
            insideTeleports.Clear(); 
            entranceTeleports = UnityEngine.Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None); //Load all Teleports
            for (int i = 0; i < entranceTeleports.Length; i++)
            {
                int entranceID = entranceTeleports[i].entranceId;

                if (entranceTeleports[i].isEntranceToBuilding)
                {
                    if (!entranceTeleports[i].FindExitPoint())
                    {
                        continue;
                    }
                    else if (entranceTeleports[i].entrancePoint == null)
                    {
                        continue;
                    }

                    outsideTeleports.Add(entranceTeleports[i]);
                    outsideTeleports.Sort((entranceA, entranceB) => entranceA.entranceId.CompareTo(entranceB.entranceId));
                }
                else
                {
                    if (!entranceTeleports[i].FindExitPoint())
                    {
                        continue;
                    }
                    else if (entranceTeleports[i].entrancePoint == null)
                    {
                        continue;
                    }

                    insideTeleports.Add(entranceTeleports[i]);
                    insideTeleports.Sort((entranceA, entranceB) => entranceA.entranceId.CompareTo(entranceB.entranceId));
                }
            }
            //End Code from StarlancerAIFix
            if (isOutside)
            {
                if (allAINodes == null || allAINodes.Length == 0)
                {
                    allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                }
                if (GameNetworkManager.Instance.localPlayerController != null)
                {
                    EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom);
                }
            }
            else if (allAINodes == null || allAINodes.Length == 0)
            {
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            }
            openDoorSpeedMultiplier = 450f;
            SetShyGuyInitialValues();
        }

        private void CalculateAnimationSpeed()
        {
            float num = (transform.position - previousPosition).magnitude;
            if (num > 0f)
            {
                num = 1f;
            }
            lastRunSpeed = Mathf.Lerp(lastRunSpeed, num, 5f * Time.deltaTime);
            creatureAnimator.SetFloat("VelocityZ", lastRunSpeed);
            previousPosition = transform.position;
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (StartOfRound.Instance.livingPlayers == 0)
            {
                lastInterval = Time.realtimeSinceStartup;
                return;
            }
            if (isEnemyDead)
            {
                lastInterval = Time.realtimeSinceStartup;
                return;
            }
            if (!IsServer && IsOwner && currentBehaviourStateIndex != 2)
            {
                ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            switch (currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (stunNormalizedTimer > 0f)
                        {
                            agent.speed = 0f;
                        }
                        else if (sitting)
                        {
                            agent.speed = 0f;
                        }
                        else
                        {
                            roamWaitTime -= Time.realtimeSinceStartup - lastInterval;
                            openDoorSpeedMultiplier = 1f;
                            agent.speed = 2.75f * Config.speedDocileMultiplier;
                        }
                        movingTowardsTargetPlayer = false;
                        agent.stoppingDistance = 4f;
                        addPlayerVelocityToDestination = 0f;
                        PlayerControllerB targetPlayer = base.targetPlayer;
                        if (roamWaitTime <= 20f && roamMap.inProgress && base.targetPlayer == null)
                        {
                            StopSearch(roamMap);
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (roamWaitTime > 2.5f && roamWaitTime <= 15f && !roamMap.inProgress && base.targetPlayer == null && roamShouldSit)
                        {
                            creatureVoice.Stop();
                            sitting = true;
                            creatureAnimator.SetBool("Sitting", value: true);
                            PlayAudioFxServerRpc(0);
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (!(base.targetPlayer != null) && targetPlayer == null && !roamMap.inProgress && roamWaitTime <= 0f)
                        {
                            if (!sitting)
                            {
                                roamShouldSit = Random.Range(1, 5) == 1;
                                roamWaitTime = Random.Range(25f, 32.5f);
                                StartSearch(spawnPosition, roamMap);
                                lastInterval = Time.realtimeSinceStartup;
                                break;
                            }
                            sitting = false;
                            creatureVoice.Stop();
                            roamShouldSit = false;
                            roamWaitTime = Random.Range(21f, 25f);
                            creatureAnimator.SetBool("Sitting", value: false);
                            PlayAudioFxServerRpc(1);
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        break;
                    }
                case 1:
                    agent.speed = 0f;
                    lastInterval = Time.realtimeSinceStartup;
                    movingTowardsTargetPlayer = false;
                    break;
                case 2:
                    {
                        agent.stoppingDistance = 0f;
                        agent.avoidancePriority = 99;
                        openDoorSpeedMultiplier = 450f;
                        mainCollider.isTrigger = true;
                        addPlayerVelocityToDestination = 1f;
                        if (inKillAnimation)
                        {
                            agent.speed = 0f;
                        }
                        else
                        {
                            agent.speed = Mathf.Clamp(agent.speed + (Time.realtimeSinceStartup - lastInterval) * Config.speedRageMultiplier * 1.1f, 5f * Config.speedRageMultiplier, 14.75f * Config.speedRageMultiplier);
                        }
                        if (SCP096Targets.Count <= 0)
                        {
                            SitDown();
                            break;
                        }
                        PlayerControllerB oldTargetPlayer = base.targetPlayer;
                        float closestDist = 0f;
                        if(!StartOfRound.Instance.shipHasLanded) { ScopophobiaPlugin.logger.LogInfo("Ship is leaving: " + StartOfRound.Instance.shipHasLanded);return; }
                        foreach (PlayerControllerB hunted in SCP096Targets)
                        {
                            bool sameArea = hunted.isInsideFactory == !isOutside;
                            bool allowedToLeave = true;
                            if (!Config.canExitFacility && !sameArea)
                            {
                                allowedToLeave = false;
                            }
                            if (!hunted.isPlayerDead && allowedToLeave)
                            {
                                bool isTargetable = PlayerIsTargetable(hunted, false, true);
                                float distance = Vector3.Distance(hunted.transform.position, base.transform.position);
                                if (!hunted.isPlayerDead && hunted.isPlayerControlled && hunted.inAnimationWithEnemy == null && hunted.sinkingValue < 0.73f && distance < float.PositiveInfinity)//manually check if player is targetable, as it blocks if players are in the ship
                                {
                                    closestDist = Vector3.Magnitude(hunted.transform.position -  base.transform.position);
                                    targetPlayer = hunted;
                                }
                            }
                            else
                            {
                                AddTargetToList((int)hunted.actualClientId, remove: true);
                            }
                        }
                        if (targetPlayer != null)
                        {
                            creatureAnimator.SetFloat("DistanceToTarget", Vector3.Distance(base.transform.position, targetPlayer.transform.position));
                            if (roamMap.inProgress)
                            {
                                StopSearch(roamMap);
                            }
                            if (targetPlayer != oldTargetPlayer)
                            {
                                ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                            }
                            if (!isOutside && RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name == "Level3Flow" && (Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.transform.position) < 3f || Vector3.Distance(base.transform.position, elevatorScript.elevatorBottomPoint.transform.position) < 3f))//Checks if Shy guy is Outside, and if the Elevator Exists in the level.
                            {//if we're at the elevator, we should try to use it.
                                TryUsingElevator();
                            }
                            if (isOutside && targetPlayer.isInHangarShipRoom && !pryingOpenDoor)
                            {
                                if (BreakIntoShip())
                                    break;
                            }
                            if (targetPlayer.isInsideFactory != !isOutside)
                            {//new code. if not already pathing, lets find closest teleport. Then move to it if not right there. Else, continue as normal.
                                if (!pathingToTeleport) { GetClosestTeleportAndMove(); }

                                if (Vector3.Distance(transform.position, closestTeleportPosition) < 2f)
                                {
                                    TeleportEnemy(closestTeleportPosition, !isOutside);
                                    agent.speed = 0f;
                                    return;
                                }
                                else
                                {
                                    movingTowardsTargetPlayer = false;
                                    SetDestinationToPosition(closestTeleportPosition);
                                }
                            }
                            else//Player in sights, continuing attack strategy
                            {
                                if (PathIsIntersectedByLineOfSight(RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 1.4f, agent.areaMask)))
                                {
                                    SetMovingTowardsTargetPlayer(targetPlayer);//we do this so we can head straight for the target position via navmesh
                                }
                                else//then we do this so that shy guy will run to the general vicinity of the player while not in LOS
                                {
                                    SetMovingTowardsTargetPlayer(targetPlayer);
                                }
                            }
                        }
                        else if (SCP096Targets.Count <= 0)
                        {
                            SitDown();
                        }
                        break;
                    }
                default:
                    lastInterval = Time.realtimeSinceStartup;
                    break;
            }
        }

        public void GetClosestTeleportAndMove()
        {
            NavMeshPath PathToTele = new NavMeshPath();
            if (!pathingToTeleport && isOutside)
            {
                foreach (EntranceTeleport tele in outsideTeleports)
                {
                    if (agent.CalculatePath(tele.entrancePoint.transform.position, PathToTele) && PathToTele.status == NavMeshPathStatus.PathComplete)//if path fails, this might take longer. Shouldn't be an issue however.
                    {
                        if (Vector3.Distance(transform.position, tele.entrancePoint.transform.position) < Vector3.Distance(transform.position, mainEntrancePosition))
                        {
                            ScopophobiaPlugin.logger.LogInfo("Path Completed");
                            ScopophobiaPlugin.logger.LogInfo("Going to closest Exit");
                            closestTeleport = tele;
                            closestTeleportPosition = tele.entrancePoint.transform.position;
                            pathingToTeleport = true;
                            break;
                        }
                    
                        else if (tele.isEntranceToBuilding)
                        {
                            ScopophobiaPlugin.logger.LogInfo("Cant Find Teleport, just using Main");
                            closestTeleport = tele;
                            closestTeleportPosition = tele.entrancePoint.transform.position;
                            pathingToTeleport = true;
                            break;
                        }
                    }
                }
            }
            else if (!pathingToTeleport && !isOutside)
            {
                foreach (EntranceTeleport tele in insideTeleports)
                {
                    if (agent.CalculatePath(tele.entrancePoint.transform.position, PathToTele) && PathToTele.status == NavMeshPathStatus.PathComplete)//if path fails, this might take a while. shouldn't be an issue however.
                    {
                        ScopophobiaPlugin.logger.LogInfo("Path Completed");
                        if (Vector3.Distance(transform.position, tele.entrancePoint.transform.position) < Vector3.Distance(transform.position, mainEntrancePosition))
                        {
                            ScopophobiaPlugin.logger.LogInfo("Going to closest Exit");
                            closestTeleport = tele;
                            closestTeleportPosition = tele.entrancePoint.transform.position;
                            pathingToTeleport = true;
                            break;
                        }
                        else if (tele.isEntranceToBuilding)
                        {
                            ScopophobiaPlugin.logger.LogInfo("Cant Find Teleport, just using Main");
                            closestTeleport = tele;
                            closestTeleportPosition = tele.entrancePoint.transform.position;
                            pathingToTeleport = true;
                            break;
                        }
                    }
                }
            }
            else { ScopophobiaPlugin.logger.LogInfo("Error: Shy Guy is unable to escape the interior"); }
        }
        
        public void TryUsingElevator()
        {
            if (isInElevatorStartRoom)
            {
                if (Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position) < 3f)//Check distance from Bottom Button
                {
                    isInElevatorStartRoom = false;
                }
            }
            else if (Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < 3f)//Another Distance check, this time for Top Button.
            {
                isInElevatorStartRoom = true;
            }
            if (!isInElevatorStartRoom)//if not in the Elevator Start Room, Need to call the Elevator
            {
                UseElevator(true);
                return;
            }
            else if (!targetPlayer.isPlayerDead && targetPlayer.isPlayerControlled && targetPlayer.isInsideFactory && Vector3.Distance(targetPlayer.transform.position, elevatorScript.elevatorTopPoint.position) > 50f)//Is the player Inside, or hiding, Lets Distance check them from the Top Point
            {
                UseElevator(false);//Player is actually inside, Probably a fire exit. Lets go back down
                return;

            }
        }
        public void TeleportEnemy(Vector3 pos, bool setOutside)
        {
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(outsideTeleports[closestTeleport.entranceId].entrancePoint.transform.position);
            if (IsOwner)
            {
                agent.enabled = false;
                transform.position = navMeshPosition;
                agent.enabled = true;
            }
            else
            {
                transform.position = navMeshPosition;
            }
            serverPosition = navMeshPosition;
            SetEnemyOutside(setOutside);
            pathingToTeleport = false;
            if (closestTeleport.doorAudios != null && closestTeleport.doorAudios.Length != 0)
            {
                closestTeleport.entrancePointAudio.PlayOneShot(closestTeleport.doorAudios[0]);
                WalkieTalkie.TransmitOneShotAudio(closestTeleport.entrancePointAudio, closestTeleport.doorAudios[0]);
            }
        }
        private bool UseElevator(bool goUp)
        {
            Vector3 vector = ((!goUp) ? elevatorScript.elevatorTopPoint.position : elevatorScript.elevatorBottomPoint.position);
            if (elevatorScript.elevatorFinishedMoving && !PathIsIntersectedByLineOfSight(elevatorScript.elevatorInsidePoint.position, calculatePathDistance: false, avoidLineOfSight: false))
            {
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(base.transform.position, elevatorScript.elevatorInsidePoint.position) < 1f && elevatorScript.elevatorMovingDown == goUp)
                {
                    elevatorScript.PressElevatorButtonOnServer(requireFinishedMoving: true);
                }
                SetDestinationToPosition(elevatorScript.elevatorInsidePoint.position);
                return true;
            }
            if (Vector3.Distance(base.transform.position, elevatorScript.elevatorInsidePoint.position) > 1f && !PathIsIntersectedByLineOfSight(vector, calculatePathDistance: false, avoidLineOfSight: false))
            {
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(base.transform.position, vector) < 1f && elevatorScript.elevatorMovingDown != goUp && !elevatorScript.elevatorCalled)
                {
                    elevatorScript.CallElevatorOnServer(goUp);
                }
                SetDestinationToPosition(vector);
                return true;
            }
            return false;
        }
        public override void Update()
        {
            if (isEnemyDead || GameNetworkManager.Instance == null)
            {
                return;
            }
            CalculateAnimationSpeed();
            if (pryingOpenDoor && inSpecialAnimation)
            {
                base.transform.position = Vector3.Lerp(base.transform.position, shipDoor.outsideDoorPoint.position, 7f * Time.deltaTime);
                base.transform.rotation = Quaternion.Lerp(base.transform.rotation, shipDoor.outsideDoorPoint.rotation, 7f * Time.deltaTime);
                pryingDoorAnimTime = Mathf.Min(pryingDoorAnimTime + Time.deltaTime / pryOpenDoorAnimLength, 1f);
                creatureAnimator.SetFloat("pryOpenDoor", pryingDoorAnimTime);
                shipDoor.shipDoorsAnimator.SetFloat("pryOpenDoor", pryingDoorAnimTime);
                creatureAnimator.SetLayerWeight(0, Mathf.Max(0f, creatureAnimator.GetLayerWeight(0) - Time.deltaTime * 5f));
                if (pryingDoorAnimTime > 0.12f)
                {
                    EnableEnemyMesh(enable: true);
                }
                BreakIntoShip();
                return;
            }
            bool canSeeFace = GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(shyGuyFace.position, Config.faceTriggerRange, 45);
            if (canSeeFace)
            {
                float shyGuyFaceDifference = Quaternion.Angle(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.rotation, shyGuyFace.rotation);
                if (!(shyGuyFaceDifference <= 145f))
                {
                    canSeeFace = false;
                }
            }
            if (canSeeFace)
            {
                seeFaceTime += Time.deltaTime;
                if (seeFaceTime >= Config.faceTriggerGracePeriod)
                {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1.25f);
                    if (!Config.hasMaxTargets || SCP096Targets.Count < Config.maxTargets)
                    {
                        AddTargetToList((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                    }
                    if (currentBehaviourStateIndex == 0)
                    {
                        SwitchToBehaviourState(1);
                    }
                }
            }
            else
            {
                seeFaceTime = Mathf.Clamp(seeFaceTime - Time.deltaTime, 0f, timeToTrigger);
                if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 1f, 30f) && currentBehaviourStateIndex == 0)
                {
                    if (!thisNetworkObject.IsOwner)
                    {
                        ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
                    }
                    if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 10f)
                    {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.65f);
                    }
                    else
                    {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.25f);
                    }
                }
            }
            switch (currentBehaviourStateIndex)
            {
                case 0:
                    if (previousState != 0)
                    {
                        SetShyGuyInitialValues();
                        previousState = 0;

                        mainCollider.isTrigger = true;
                        farAudio.volume = 0f;
                        creatureVoice.Stop();
                        PlayAudioFxServerRpc(0);
                    }
                    if (roamMap.inProgress)
                    {
                        creatureVoice.Stop();
                        PlayAudioFxServerRpc(1);
                    }
                    break;
                case 1:
                    if (previousState != 1)
                    {
                        previousState = 1;
                        sitting = false;
                        mainCollider.isTrigger = true;
                        creatureAnimator.SetBool("Rage", value: false);
                        creatureAnimator.SetBool("Sitting", value: false);
                        creatureAnimator.SetBool("triggered", value: true);
                        creatureVoice.Stop();
                        PlayAudioFxServerRpc(2);
                        agent.speed = 0f;

                        triggerTime = triggerDuration;
                    }
                    triggerTime -= Time.deltaTime;
                    if (triggerTime <= 0f)
                    {
                        SwitchToBehaviourState(2);
                    }
                    break;
                case 2:
                    mainCollider.isTrigger = true;
                    if (previousState != 2)
                    {
                        mainCollider.isTrigger = true;
                        previousState = 2;
                        creatureAnimator.SetBool("Rage", value: true);
                        creatureAnimator.SetBool("triggered", value: false);
                        farAudio.Stop();
                        PlayAudioFxServerRpc(3);
                    }
                    break;
            }
            base.Update();
        }

        [ServerRpc]
        public void PlayAudioFxServerRpc(int audioClipID)
        {
            PlayAudioFXClientRpc(audioClipID);
        }
        [ClientRpc]
        public void PlayAudioFXClientRpc(int audioClipID)
        {
            switch (audioClipID)
            {
                case 0: farAudio.Stop(); creatureVoice.Stop(); creatureVoice.volume = Config.VolumeConfigs * 0.1f; creatureVoice.clip = crySittingSFX; creatureVoice.loop = true; float preTime = creatureVoice.time; creatureVoice.time = preTime; creatureVoice.Play(); break;
                case 1: creatureVoice.Stop(); creatureVoice.volume = Config.VolumeConfigs * 0.1f; creatureVoice.clip = crySFX; creatureVoice.loop = true; float preTime2 = creatureVoice.time; creatureVoice.time = preTime2; creatureVoice.Play(); break;
                case 2: creatureVoice.Stop(); farAudio.volume = Config.VolumeConfigs * 0.1f; farAudio.clip = panicSFX; float preTime3 = farAudio.time; farAudio.loop = true; farAudio.time = preTime3; farAudio.Play(); break;
                case 3: farAudio.Stop(); farAudio.volume = Config.VolumeConfigs * 0.1f - 0.1f; farAudio.clip = screamSFX; float preTime4 = farAudio.time; farAudio.loop = true; farAudio.time = preTime4; farAudio.Play(); break;
            }
        }

        public new void SetEnemyOutside(bool outside = false)
        {
            isOutside = outside;
            mainEntrancePosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, outside));
            if (!outside) allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            else allAINodes = GameObject.FindGameObjectsWithTag("AINode");
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (!other.gameObject.GetComponent<PlayerControllerB>())
            {
                return;
            }
            base.OnCollideWithPlayer(other);
            if (!inKillAnimation && !isEnemyDead && currentBehaviourStateIndex == 2)
            {
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
                if (playerControllerB != null)
                {
                    inKillAnimation = true;
                    StartCoroutine(killPlayerAnimation((int)playerControllerB.playerClientId));
                    KillPlayerServerRpc((int)playerControllerB.playerClientId);
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void KillPlayerServerRpc(int playerId)
        {
            KillPlayerClientRpc(playerId);
        }

        [ClientRpc]
        private void KillPlayerClientRpc(int playerId)
        {
            StartCoroutine(killPlayerAnimation(playerId));
        }

        private IEnumerator killPlayerAnimation(int playerId)
        {
            inKillAnimation = true;
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (isOutside && transform.position.y < -80f)
            {
                SetEnemyOutside();
            }
            else if (!isOutside && transform.position.y > -80f)
            {
                SetEnemyOutside(outside: true);
            }
            int preAmount = SCP096Targets.Count;
            playerScript.KillPlayer(playerScript.transform.position, spawnBody: true, CauseOfDeath.Mauling, 1);
            AddTargetToList(playerId, remove: true);
            creatureSFX.clip = killPlayerSFX;
            creatureSFX.Play();
            creatureAnimator.SetInteger("TargetsLeft", preAmount - 1);
            creatureAnimator.SetTrigger("kill");
            if (preAmount - 1 <= 0)
            {
                SitDown();
            }
            if (Config.deathMakesBloody && bloodyMaterial != null)
            {
                Transform model = transform.Find("SCP096Model");
                if (model != null)
                {
                    Transform modelMesh = model.Find("tsg_placeholder");
                    if (modelMesh != null)
                    {
                        SkinnedMeshRenderer skinnedModel = modelMesh.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedModel != null)
                        {
                            skinnedModel.material = bloodyMaterial;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1f);
            inKillAnimation = false;
        }

        public void SitDown()
        {
            SwitchToBehaviourState(0);
            SitDownOnLocalClient();
            SitDownServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SitDownServerRpc()
        {
            SitDownClientRpc();
        }

        [ClientRpc]
        private void SitDownClientRpc()
        {
            SitDownOnLocalClient();
        }

        public void SitDownOnLocalClient()
        {
            sitting = true;
            roamWaitTime = UnityEngine.Random.Range(45f, 50f);
            creatureAnimator.SetBool("Rage", value: false);
            creatureAnimator.SetBool("Sitting", value: true);
        }

        public void AddTargetToList(int playerId, bool remove = false)
        {
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (remove)
            {
                if (!SCP096Targets.Contains(playerScript))
                {
                    return;
                }
            }
            else if (SCP096Targets.Contains(playerScript))
            {
                return;
            }
            AddTargetToListOnLocalClient(playerId, remove);
            AddTargetToListServerRpc(playerId, remove);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddTargetToListServerRpc(int playerId, bool remove)
        {
            AddTargetToListClientRpc(playerId, remove);
        }

        [ClientRpc]
        public void AddTargetToListClientRpc(int playerId, bool remove)
        {
            AddTargetToListOnLocalClient(playerId, remove);
        }

        public void AddTargetToListOnLocalClient(int playerId, bool remove)
        {
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (remove)
            {
                if (SCP096Targets.Contains(playerScript))
                {
                    SCP096Targets.Remove(playerScript);
                }
            }
            else if (!SCP096Targets.Contains(playerScript))
            {
                SCP096Targets.Add(playerScript);
            }
        }

        private void BeginPryOpenDoor()
        {
            StartPryOpenDoorAnimationOnLocalClient();
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        }

        private void FinishPryOpenDoor(bool cancelledEarly)
        {
            FinishPryOpenDoorAnimationOnLocalClient(cancelledEarly);
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, finishAnim: true, cancelledEarly);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PryOpenDoorServerRpc(int playerWhoSent, bool finishAnim = false, bool cancelledEarly = false)
        {
            PryOpenDoorClientRpc(playerWhoSent, finishAnim, cancelledEarly);
        }

        [ClientRpc]
        public void PryOpenDoorClientRpc(int playerWhoSent, bool finishAnim = false, bool cancelledEarly = false)
        {
            if (!finishAnim)
            {
                StartPryOpenDoorAnimationOnLocalClient();
            }
            else
            {
                FinishPryOpenDoorAnimationOnLocalClient(cancelledEarly);
            }
        }

        private void FinishPryOpenDoorAnimationOnLocalClient(bool cancelledEarly = false)
        {
            if (!cancelledEarly)
            {
                shipDoor.shipDoorsAnimator.SetBool("Closed", value: false);
                StartOfRound.Instance.SetShipDoorsClosed(closed: false);
                StartOfRound.Instance.SetShipDoorsOverheatLocalClient();
                shipDoor.doorPower = 0f;
            }
            pryingOpenDoor = false;
            inSpecialAnimation = false;
            creatureAnimator.SetBool("PryingOpenDoor", value: false);
            shipDoor.shipDoorsAnimator.SetBool("PryingOpenDoor", value: false);
            creatureAnimator.SetLayerWeight(0, 1f);
        }

        private void StartPryOpenDoorAnimationOnLocalClient()
        {
            agent.enabled = false;
            pryingOpenDoor = true;
            inSpecialAnimation = true;
            creatureAnimator.SetBool("PryingOpenDoor", value: true);
            shipDoor.shipDoorsAnimator.SetBool("PryingOpenDoor", value: true);
            shipDoor.shipDoorsAnimator.SetFloat("pryOpenDoor", 0f);
            breakDownDoorAudio.PlayOneShot(breakAndEnter);
            WalkieTalkie.TransmitOneShotAudio(breakDownDoorAudio, breakAndEnter);
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 15f, 0.9f);
            StartOfRound.Instance.speakerAudioSource.PlayOneShot(shipAlarm);
            WalkieTalkie.TransmitOneShotAudio(StartOfRound.Instance.speakerAudioSource, shipAlarm);
            if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) < 18f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            }
        }
        public bool BreakIntoShip()
        {
            if (shipDoor == null)
            {
                Debug.LogError("Scopophobia error: ship door is null");
                return false;
            }
            if (pryingOpenDoor)
            {
                if (pryingDoorAnimTime >= 1f)
                {
                    FinishPryOpenDoor(cancelledEarly: false);
                }
                return true;
            }
            if (StartOfRound.Instance.hangarDoorsClosed && StartOfRound.Instance.shipStrictInnerRoomBounds.bounds.Contains(destination) && Vector3.Distance(base.transform.position, shipDoor.outsideDoorPoint.position) < 4f)//revert to 4f
            {
                BeginPryOpenDoor();
                return true;
            }
            return false;
        }
        private void SetShyGuyInitialValues()
        {
            mainCollider = gameObject.GetComponentInChildren<Collider>();
            farAudio = transform.Find("FarAudio").GetComponent<AudioSource>();
            creatureVoice = transform.Find("CreatureVoice").GetComponent<AudioSource>();
            targetPlayer = null;
            inKillAnimation = false;
            pryingOpenDoor = false;
            SCP096Targets.Clear();
            creatureAnimator.SetFloat("VelocityX", 0f);
            creatureAnimator.SetFloat("VelocityZ", 0f);
            creatureAnimator.SetFloat("DistanceToTarget", 999f);
            creatureAnimator.SetFloat("pryOpenDoor", 999f);
            creatureAnimator.SetInteger("SitActionTimer", 0);
            creatureAnimator.SetInteger("TargetsLeft", 0);
            creatureAnimator.SetBool("Rage", value: false);
            creatureAnimator.SetBool("Sitting", value: false);
            creatureAnimator.SetBool("triggered", value: false);
            creatureAnimator.SetBool("PryingOpenDoor", value: false);
            mainCollider.isTrigger = true;
            farAudio.volume = 0f;
            farAudio.Stop();
            creatureVoice.Stop();
        }
    }
}