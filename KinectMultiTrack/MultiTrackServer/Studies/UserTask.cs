﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMultiTrack.Studies
{
    public class UserTask {

        // Stand
        public static string STAND_STILL = "Stand still";

        // Walk
        public static string LEFT = "Move Left";
        public static string RIGHT = "Move Right";
        public static string BACKWARD = "Move Backward";
        public static string FORWARD = "Move Forward";
        public static string START_POSITION = "Move to the\nStarting Position";
        public static string SQUARE = "Go Around the\nSquare (Clockwise)";
        public static string TOP_RIGHT = "Go to Top Right";
        public static string BOT_LEFT = "Go to Bottom Left";
        public static string TOP_LEFT = "Go to Top Left";
        public static string BOT_RIGHT = "Go to Bottom Right";

        // Interaction
        public static string PERSON_1_WALK = "Person 1\nWalks Past\nPerson 2";
        public static string PERSON_1_AROUND = "Person 1\nGoes Around\nPerson 2";
        public static string PERSON_2_WALK = "Person 2\nWalks Past\nPerson 1";
        public static string PERSON_2_AROUND = "Person 2\nGoes Around\nPerson 1";
        public static string PERSONS_EXCHANGE = "Exchange Positions";

        // Occlusion
        public static string GO_AROUND_OCCLUSION = "Go Around the\nObstacle";

        // Instruction
        public static string READY = "Ready";
        public static string DONE = "Done!!!";

        public const int STATIONARY_DURATION_SEC = 5;

        public string Description { get; private set; }
        public int ScenarioId { get; private set; }

        public UserTask(string description, int scenarioId)
        {
            this.Description = description;
            this.ScenarioId = scenarioId;
        }

        public static readonly IEnumerable<UserTask> TASK_EMPTY = new List<UserTask>();

        public static readonly IEnumerable<UserTask> TASK_STATIONARY = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.STAND_STILL, Logger.SCENARIO_STATIONARY),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_WALK_WEI = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.FORWARD, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.LEFT, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.RIGHT, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.BACKWARD, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.BACKWARD+"\n(Again)", Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.LEFT, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.BACKWARD, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.RIGHT, Logger.SCENARIO_WALK_WEI),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_WALK_CURRENT = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.START_POSITION, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.SQUARE, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.TOP_RIGHT, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.BOT_LEFT, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.TOP_LEFT, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.BOT_RIGHT, Logger.SCENARIO_WALK_CURRENT),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_FIRST_3 = UserTask.TASK_STATIONARY.Concat(UserTask.TASK_WALK_WEI).Concat(UserTask.TASK_WALK_CURRENT);

        public static readonly IEnumerable<UserTask> TASK_INTERACTION_1 = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.PERSON_1_WALK, Logger.SCENARIO_INTERACTION_1_P1),
            new UserTask(UserTask.PERSON_1_AROUND, Logger.SCENARIO_INTERACTION_1_P1),
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.PERSON_2_WALK, Logger.SCENARIO_INTERACTION_1_P2),
            new UserTask(UserTask.PERSON_2_AROUND, Logger.SCENARIO_INTERACTION_1_P2),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_INTERACTION_2 = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.PERSONS_EXCHANGE, Logger.SCENARIO_INTERACTION_2),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_INTERACTIONS = UserTask.TASK_INTERACTION_1.Concat(UserTask.TASK_INTERACTION_2);

        public static readonly IEnumerable<UserTask> TASK_OCCLUSION_1 = new List<UserTask>()
        {
            new UserTask(UserTask.READY, Logger.SCENARIO_NA),
            new UserTask(UserTask.GO_AROUND_OCCLUSION, Logger.SCENARIO_OCCLUSION_1),
            new UserTask(UserTask.DONE, Logger.SCENARIO_NA)
        };

        public static readonly IEnumerable<UserTask> TASK_FREE = new List<UserTask>()
        {
            new UserTask("", Logger.SCENARIO_FREE)
        };
    }

}
