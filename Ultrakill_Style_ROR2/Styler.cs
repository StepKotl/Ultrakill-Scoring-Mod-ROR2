using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Security.Authenticode;
using R2API;
using R2API.Utils;
using Rewired;
using RoR2;
using RoR2.Orbs;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.WSA;
using TMPro;
using ExamplePlugin;

namespace ExamplePlugin
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]
    
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class THPSScoring : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "StepKotl";
        public const string PluginName = "THPSScoring";
        public const string PluginVersion = "0.1.0";

        public double totalScore = 0;
        public int airScore = 1;
        public bool scoringActive = true;
        public bool onStage;

        // Ultrakill Scoring
        // Points for each rank
        public List<int> points = [0, 200, 300, 400, 500, 700, 850, 1000, 1500];
        // Rate of decay
        public List<double> decayMult = [1, 1, 1.25, 1.5, 2, 3, 4, 6, 8];
        // File locations for each texture
        public List<string> ranksPath = ["ExamplePlugin\\Textures\\RankD.png","ExamplePlugin\\Textures\\RankD.png","ExamplePlugin\\Textures\\RankC.png","ExamplePlugin\\Textures\\RankB.png","ExamplePlugin\\Textures\\RankA.png","ExamplePlugin\\Textures\\RankS.png","ExamplePlugin\\Textures\\RankSS.png","ExamplePlugin\\Textures\\RankSSS.png","ExamplePlugin\\Textures\\RankU.png"];

        public List<Sprite> ranksSprites = [];


        public int level = 0;
        public HUD UI;
        public string runActive = "NS";
        public TextMeshProUGUI txt;
        public Image img;        




        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);



            

            //hooking onto the UI elements
            On.RoR2.UI.HUD.Awake += MyFunc;
            


            // Defining onEvents for damage and kills
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt; 
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            // Defining 
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onServerGameOver += Run_onServerGameOver;
            Stage.onStageStartGlobal += Stage_onStageStartGlobal;


            // Converting the images into sprites
            for (int i = 0; i < 10; i++ ){
                string currentPath = ranksPath[i];
                ranksSprites.Add(IMG2Sprite.LoadNewSprite(currentPath, 100.0f));
                
            }
                       

        }

        private void Stage_onStageStartGlobal(Stage stage){totalScore = 0;}
        // When the stage changes, the counter should reset. 


        private void Run_onRunStartGlobal(Run run){runActive = "T";}
        private void Run_onServerGameOver(Run run, GameEndingDef def){runActive = "T";}
        // the two above lines are used for starting and stopping the counter, and only when the run is active. 



        // On the death of any character/enemy
        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report) {

            // if the attacker was the player of the current client, increase the multiplier (eventually depending on the type of enemy)
            if (PlayerCharacterMasterController._instances[0].body.gameObject == report.attacker){
                Log.Info($"Boss {report.victimIsBoss} | Elite {report.victimIsElite} | Champion {report.victimIsChampion}");
            }
        }


        //On an instance of damage
        private void GlobalEventManager_onServerDamageDealt(DamageReport report){
            
            var CurrentDamage = PlayerCharacterMasterController.instances[0].body.damage;
            //Check if the damage was done by the player of the current client
            if (PlayerCharacterMasterController._instances[0].body.gameObject == report.attacker.gameObject){   
                
                double mult = 7.5;

                // If the enemy attacked is a boss, multiply the amount of damage done by 1.5
                if (report.victimIsBoss){    
                    mult *= 1.5;
                }


                // If the enemy is an Elite, multiply the amount of damage done by 1.25
                else if (report.victimIsElite){                    
                    mult *= 1.25;
                }
                
                totalScore += mult*(report.damageDealt/CurrentDamage);

                
                
                
            }


            if (PlayerCharacterMasterController._instances[0].body.gameObject == report.victim.gameObject){
                totalScore -= report.damageDealt*5;

            }

        }

        

        //Commands that repeat every frame
        private void Update()
        {
        
            // Increase the level when there are enough points and if it can 
            if (level < 8){
                if (totalScore >= points[level]){
                    level += 1;
                }
            }


            // Decrease the level if it can 
            if (level > 0){
                if (totalScore <= points[level - 1]){
                    level -= 1;
                }
            }


            
            if (runActive == "T"){

                // Score Decay
                if (totalScore >= 0){
                    totalScore -= 15*decayMult[level]*Time.deltaTime*airScore;
                }


                // Score Min Reached
                if (totalScore < 0){
                    totalScore = 0;
                }

                // Rounding and Updating Score
                string currentScore = Math.Round(totalScore, 0).ToString();
                txt.text = $"<voffset=-2em><align=left><b>{currentScore}</b></voffset> \n Level = {level}";
                img.sprite = ranksSprites[level];
            }

            if (runActive == "F"){
                txt.text = "";
            }
        }


        //Using the UI hook
        private void MyFunc(On.RoR2.UI.HUD.orig_Awake orig, HUD self)
        {
            orig(self);
            UI = self;

            

            //hud.mainContainer.transform // This will return the main container. You should put your UI elements under it or its children!
            new GameObject("ScoreUI");
            GameObject myObject = GameObject.Find("ScoreUI");
            myObject.transform.SetParent(UI.mainContainer.transform);
            RectTransform rectTransform = myObject.AddComponent<RectTransform>();
            
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            txt = myObject.AddComponent<TextMeshProUGUI>();

            img = myObject.AddComponent<Image>();
            txt.text = "";
            
        }
        
        //UI Unhook
        private void OnDestroy()
        {
        On.RoR2.UI.HUD.Awake -= MyFunc;
        }
    }
}


/* Features to add:
- Multiplier with air time
- Counter decay on damage
- Visuals 

*/