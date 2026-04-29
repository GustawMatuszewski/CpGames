// ============================================================
//  ItemDatabasePopulator.cs
//  Place this file in any  Editor/  folder inside your Unity project.
//  Then open:  Tools → Item Database Populator
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ItemDatabasePopulator : EditorWindow
{
    // ── window ──────────────────────────────────────────────
    [MenuItem("Tools/Item Database Populator")]
    public static void ShowWindow() => GetWindow<ItemDatabasePopulator>("Item DB Populator");

    private Vector2 scroll;

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Item Database Populator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Finds every Item asset in your project and overwrites its itemName + description " +
            "with the English values defined in this script. " +
            "Nothing else (stats, type, sprite…) is touched.",
            MessageType.Info);
        EditorGUILayout.Space(8);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (GUILayout.Button("▶  Run – populate all items", GUILayout.Height(36)))
            RunPopulator();

        EditorGUILayout.EndScrollView();
    }
// Zamienia ą -> a, ł -> l, itd. oraz usuwa zbędne znaki (kropki, myślniki)
    string SuperNormalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        string text = input.ToLower();
        
        // Usuwamy spacje, podłogi, myślniki i kropki, aby maksymalnie ujednolicić nazwy
        text = text.Replace("_", "").Replace(" ", "").Replace("-", "").Replace(".", "");
        
        string from = "ąćęłńóśźż";
        string to   = "acelnoszz";
        for (int i = 0; i < from.Length; i++)
            text = text.Replace(from[i], to[i]);
            
        return text;
    }

    // ── main logic ──────────────────────────────────────────
    static void RunPopulator()
    {
        var rawData = BuildDataMap();
        var normalizedData = new Dictionary<string, (string name, string description)>();
        
        var instance = CreateInstance<ItemDatabasePopulator>();

        // Tworzymy pomocniczy słownik. 
        // Zapisujemy pod znormalizowanym kluczem PL, ale TAKŻE pod kluczem EN.
        // Dzięki temu skrypt nie wywali błędu na plikach, które już wcześniej zmieniły nazwę.
        foreach (var kvp in rawData)
        {
            string cleanPolishKey = instance.SuperNormalize(kvp.Key);
            string cleanEnglishKey = instance.SuperNormalize(kvp.Value.name);

            normalizedData[cleanPolishKey] = kvp.Value;
            
            if (!normalizedData.ContainsKey(cleanEnglishKey))
            {
                normalizedData[cleanEnglishKey] = kvp.Value;
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:Item");
        int updated = 0, skipped = 0, alreadyDone = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item == null) continue;

            // Używamy item.name, a nie Path, aby uniknąć problemów z ucinaniem np. "1.5L"
            string fileName = item.name; 
            string cleanFileName = instance.SuperNormalize(fileName);

            if (normalizedData.TryGetValue(cleanFileName, out var entry))
            {
                bool needsSave = false;

                // Podmieniamy dane wewnętrzne tylko jeśli faktycznie się różnią
                if (item.itemName != entry.name || item.description != entry.description)
                {
                    Undo.RecordObject(item, "Populate Item EN");
                    item.itemName = entry.name;
                    item.description = entry.description;
                    EditorUtility.SetDirty(item);
                    needsSave = true;
                }

                // Zmieniamy nazwę pliku tylko, jeśli jeszcze tego nie zrobiliśmy
                if (fileName != entry.name)
                {
                    string renameResult = AssetDatabase.RenameAsset(path, entry.name);
                    
                    if (string.IsNullOrEmpty(renameResult))
                    {
                        Debug.Log($"<color=green>[OK]</color> Zmieniono: {fileName} -> {entry.name}");
                        updated++;
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow>[Zaktualizowano dane, Błąd nazwy]</color> {fileName}: {renameResult}");
                        updated++; 
                    }
                }
                else
                {
                    // Plik nazywa się poprawnie
                    if (needsSave) updated++;
                    else alreadyDone++;
                }
            }
            else
            {
                skipped++;
                // Lepszy log błędu, który od razu pokaże Ci problematyczną nazwę
                Debug.LogError($"<color=red>[BRAK W SŁOWNIKU]</color> Plik w Unity: '{fileName}' | Znormalizowany klucz: '{cleanFileName}'. Sprawdź czy nie ma literówki w słowniku!");
            }
        }

        DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Wynik operacji", 
            $"Zaktualizowano (lub zmieniono nazwę): {updated}\n" +
            $"Było już gotowe (pominięto): {alreadyDone}\n" +
            $"Brak w słowniku (nieznalezione): {skipped}", "OK");
    }

    // ── data map ────────────────────────────────────────────
    // Key   = exact filename (no extension), lowercased, spaces→underscores kept as-is
    // Value = (English display name, English description)

    static Dictionary<string, (string name, string description)> BuildDataMap()
    {
        var d = new Dictionary<string, (string, string)>(System.StringComparer.OrdinalIgnoreCase);

        // ────────────────────────────────────────────────────
        //  WEAPONS  –  Items\weapons
        // ────────────────────────────────────────────────────
        d["deska_z_gwozdziami"]    = ("NailBoard",           "A plank with rusty nails hammered through it. Crude but effective in a pinch.");
        d["dluto"]                 = ("WoodChisel",           "A sharp-edged woodworking tool. Can split skulls as easily as timber.");
        d["gazrurka"]              = ("GasPipe",              "A heavy steel pipe that once carried gas. Dense and balanced for swinging.");
        d["grabie"]                = ("GardenRake",           "Long-handled rake with metal tines. Keeps enemies at arm's length.");
        d["hantel"]                = ("Dumbbell",              "A cast-iron weight. Heavy, durable, and delivers bone-crushing blows.");
        d["kij_baseballowy"]       = ("BaseballBat",          "Classic wooden bat. A survivor's staple – reliable and satisfying.");
        d["kij_do_krykieta"]       = ("CricketBat",           "Wide flat bat made of willow. Solid for a broad sweeping swing.");
        d["kij_golfowy"]           = ("GolfClub",             "Iron-shafted golf club. The last course it played is a different kind of rough.");
        d["kij_z_nozem"]           = ("KnifeTippedStaff",    "A wooden staff with a knife lashed to the end. Improvised spear.");
        d["kilof"]                 = ("Pickaxe",               "Mining pick with a steel head. Penetrates light armour with ease.");
        d["klucz_do_kol"]          = ("LugWrench",            "Heavy cross-shaped wrench. Four-sided grip makes for unpredictable swings.");
        d["klucz_francuski"]       = ("AdjustableWrench",     "Standard mechanic's wrench. Adjustable jaw doubles as a grip weapon.");
        d["lopata"]                = ("Shovel",                "Full-sized digging shovel. The flat blade cleaves on the backswing.");
        d["maczeta"]               = ("Machete",               "Broad-bladed clearing blade. Cuts through brush and bone alike.");
        d["noz_do_tapet"]          = ("BoxCutter",            "Retractable blade used for wallpaper and packaging. Razor-sharp.");
        d["nozyce_do_metalu"]      = ("MetalShears",          "Heavy-duty cutting shears for sheet metal. Shear through thin material fast.");
        d["otwieracz_do_konserw"]  = ("CanOpener",            "Mechanical can opener repurposed as a puncturing tool.");
        d["palka_policyjna"]       = ("PoliceBaton",          "Regulation-length baton. Designed to subdue, not kill – but can do both.");
        d["patelnia"]              = ("FryingPan",            "Cast-iron skillet. Heavy enough to knock someone out cold.");
        d["sekator"]               = ("PruningShears",        "Spring-loaded garden shears. Compact and devastating at close range.");
        d["tluczek_do_miesa"]      = ("MeatTenderizer",       "Spiked mallet for tenderising meat. Works on more than just steak.");
        d["wedka"]                 = ("FishingRod",           "Flexible fibreglass rod. Useful for fishing or keeping distance.");
        d["widelki_ogrodowe"]      = ("GardenFork",           "Four-tined garden fork. Penetrates soft targets with multiple prongs.");
        d["zelazko"]               = ("ClothesIron",          "Electric iron – unplugged but still a solid hunk of metal.");

        // ────────────────────────────────────────────────────
        //  WEAPONS  –  Items\weapons\default
        // ────────────────────────────────────────────────────
        d["bayonet"]               = ("Bayonet",               "Military bayonet blade. Mounts on a rifle or used alone as a combat knife.");
        d["butterfly knife"]       = ("ButterflyKnife",       "Folding knife with split handle. Fast to deploy in trained hands.");
        d["dwu ręczna siekiera"]   = ("TwoHandedAxe",        "Large felling axe requiring both hands. Massive damage, slow swing.");
        d["lom"]                   = ("Crowbar",               "Solid steel crowbar. Forces open doors and jaws alike.");
        d["metalowy_pret"]         = ("MetalRod",             "Length of solid steel rod. Cheap, heavy, and unbreakable.");
        d["mlotek_ciesielski"]     = ("ClawHammer",           "Carpenter's hammer with a forked claw. Pulls nails, cracks skulls.");
        d["noz_kuchenny"]          = ("KitchenKnife",         "Sharp chef's knife. Quick and precise – familiar in any hand.");
        d["noz_mysliwski"]         = ("HuntingKnife",         "Full-tang fixed-blade knife. Designed for field dressing – or worse.");
        d["pila_reczna"]           = ("HandSaw",              "Crosscut saw with aggressive teeth. Slow but devastatingly effective.");
        d["plaski_srubokret"]      = ("FlatHeadScrewdriver", "Standard flat-blade screwdriver. Doubles as a puncturing tool.");
        d["scissors"]              = ("Scissors",              "Large household scissors. Sharp enough to cut skin as easily as fabric.");
        d["siekiera"]              = ("Hatchet",               "One-handed camp hatchet. Compact and lethal at close range.");
        d["siekiera_strazacka"]    = ("FireAxe",              "Heavy-duty rescue axe. Breaks doors and barriers – or attackers.");
        d["srubokret"]             = ("PhillipsScrewdriver",  "Cross-head screwdriver. A decent stabbing implement when needed.");
        d["tasak_rzeznicki"]       = ("ButchersCleaver",     "Thick-spined cleaver designed to split bone. Terrifying in combat.");
        d["zapalniczka"]           = ("Lighter",               "Disposable flame lighter. Can ignite materials or threaten attackers.");

        // ────────────────────────────────────────────────────
        //  CLOTHING  –  Items  (root)
        // ────────────────────────────────────────────────────
        d["bluza"]                   = ("Hoodie",                   "Plain cotton hoodie. Comfortable and warm for cool weather.");
        d["dog training armor"]      = ("DogTrainingArmour",      "Padded sleeve armour used in K-9 training. Bite-resistant.");
        d["dzins bluza"]             = ("DenimJacketHoodie",      "Denim-shelled hoodie hybrid. Tough outer layer with soft lining.");
        d["dzins jacked"]            = ("DenimJacket",             "Classic denim jacket. Light protection against scrapes and wind.");
        d["firefighter jacked"]      = ("FirefighterJacket",       "Flame-retardant turnout coat. Heavy but extremely protective.");
        d["glasses"]                 = ("Glasses",                  "Prescription or safety glasses. Protects eyes from debris.");
        d["hat cowboy"]              = ("CowboyHat",               "Wide-brimmed felt hat. Shields from sun and light rain.");
        d["hat woolen"]              = ("WoolHat",                 "Knitted wool beanie. Retains warmth even when damp.");
        d["helmet firefighter"]      = ("FirefighterHelmet",       "Hard-shell helmet with face shield. Excellent head protection.");
        d["koszula"]                 = ("Shirt",                    "Standard collared shirt. Light, breathable, everyday wear.");
        d["krotki rekaw"]            = ("ShortSleeveShirt",       "Lightweight short-sleeve top. Minimal protection but keeps cool.");
        d["leather jacked"]          = ("LeatherJacket",           "Thick hide jacket. Good scratch and bite resistance.");
        d["light jacked"]            = ("LightJacket",             "Thin windproof jacket. Better than nothing against the elements.");
        d["miliitary shoes"]         = ("MilitaryBoots",           "Ankle-high combat boots. Sturdy sole and ankle support.");
        d["paper arm plate"]         = ("PaperArmGuard",          "Layered paper plate strapped to the arm. Minimal scratch protection.");
        d["paper leg plate"]         = ("PaperLegGuard",          "Layered paper wrapped around the leg. Very light protection.");
        d["pidzama"]                 = ("Pyjamas",                  "Two-piece sleep suit. Offers almost no protection.");
        d["running shoes"]           = ("RunningShoes",            "Lightweight trainers built for speed over rough terrain.");
        d["shoes"]                   = ("CasualShoes",             "Standard flat-soled shoes. Comfortable for walking.");
        d["skurzana bluza"]          = ("LeatherHoodie",           "Hoodie cut from thick leather. Resistant to bites and scratches.");
        d["socks long"]              = ("LongSocks",               "Knee-high cotton socks. Reduces friction and blister risk.");
        d["socks short"]             = ("ShortSocks",              "Ankle socks. Basic foot protection.");
        d["spodnie defoult"]         = ("DefaultTrousers",         "Plain cotton trousers. Everyday wear with minimal protection.");
        d["spodnie dzins"]           = ("DenimJeans",              "Thick denim jeans. Decent scratch resistance.");
        d["spodnie leather"]         = ("LeatherTrousers",         "Hardwearing leather trousers. Good abrasion protection.");
        d["szalik"]                  = ("Scarf",                    "Woollen scarf. Keeps the neck warm and can cover the face.");
        d["underware"]               = ("Underwear",                "Basic cotton underwear. The foundation of any outfit.");
        d["winter jacked"]           = ("WinterJacket",            "Insulated padded jacket. Essential in freezing temperatures.");
        d["woden arm plate"]         = ("WoodenArmGuard",         "Flat wood piece strapped to the forearm. Blocks light strikes.");
        d["woden chest plate"]       = ("WoodenChestPlate",       "Carved wooden plate worn over the chest. Primitive but rigid.");
        d["woden leg plate"]         = ("WoodenLegGuard",         "Wooden slat strapped to the shin. Reduces bite and scratch damage.");

        // ────────────────────────────────────────────────────
        //  FOOD & DRINK  –  Items\Food drink
        // ────────────────────────────────────────────────────
        d["arbuzy"]                  = ("Watermelon",               "Large juicy fruit. Excellent hydration; heavy to carry.");
        d["banan"]                   = ("Banana",                   "Ripe banana. Quick energy from natural sugars.");
        d["baniak_z_woda_5l"]        = ("WaterJug5L",             "Large plastic jug of clean water. Heavy but crucial for survival.");
        d["bekon"]                   = ("Bacon",                    "Strips of cured pork belly. High in fat and flavour.");
        d["bimber"]                  = ("Moonshine",                "Illicitly distilled spirit. High alcohol content; calms the nerves dangerously.");
        d["bochenek_chleba"]         = ("BreadLoaf",               "Whole baked loaf. Dense carbohydrate source.");
        d["brokuly"]                 = ("Broccoli",                 "Fresh green broccoli. Nutritious but perishable.");
        d["brzoskwinie"]             = ("Peaches",                  "Soft summer fruit. Sweet and hydrating.");
        d["buraki"]                  = ("Beetroot",                 "Dark-red root vegetable. Rich in minerals.");
        d["butelka_z_woda_0_5l"]     = ("WaterBottle0.5L",        "Small plastic water bottle. Easy to carry.");
        d["butelka_z_woda_1_5l"]     = ("WaterBottle1.5L",        "Standard water bottle. Enough for a few hours.");
        d["butelka_z_woda_2l"]       = ("WaterBottle2L",          "Large water bottle. Solid daily supply.");
        d["coca_cola_z_cukrem"]      = ("CocaColaRegular",        "Classic carbonated soft drink. Sugar rush and short hydration.");
        d["coca_cola_zero"]          = ("CocaColaZero",           "Sugar-free cola. Hydrates without the caloric hit.");
        d["cukier"]                  = ("Sugar",                    "Granulated white sugar. Energy boost or preserving ingredient.");
        d["czekolada"]               = ("ChocolateBar",            "Compact chocolate block. Dense calories and a morale lift.");
        d["denaturat"]               = ("MethylatedSpirit",        "Denatured alcohol. Do not consume – use as fuel or disinfectant.");
        d["donuty"]                  = ("Doughnuts",                "Fried dough rings glazed with sugar. High calorie comfort food.");
        d["dzdrownice"]              = ("Earthworms",               "A handful of earthworms. Technically edible protein in desperation.");
        d["dzem_malinowy"]           = ("RaspberryJam",            "Sweet raspberry preserve. High sugar; good on bread.");
        d["fanta"]                   = ("Fanta",                    "Orange-flavoured soft drink. Sweet and energising.");
        d["faworki"]                 = ("AngelWings",              "Fried pastry strips dusted with icing sugar. Fragile but calorie-dense.");
        d["filet_z_kurczaka"]        = ("ChickenFillet",           "Raw chicken breast. Must be cooked before eating.");
        d["gorzalka"]                = ("Spirits",                  "Generic strong liquor. Warms the body; impairs judgement.");
        d["gumy_do_zucia"]           = ("ChewingGum",              "Pack of mint gum. No nutritional value; freshens breath.");
        d["jablko"]                  = ("Apple",                    "Crisp fresh apple. Light snack with natural sugars.");
        d["karma_dla_zwierzat"]      = ("PetFood",                 "Tinned pet food. Edible by humans in desperation. Not pleasant.");
        d["kawa_gotowa"]             = ("InstantCoffee",           "Pre-made coffee. Caffeine boost to fight fatigue.");
        d["kawa_ziarna"]             = ("CoffeeBeans",             "Whole roasted beans. Must be ground and brewed.");
        d["keczup"]                  = ("Ketchup",                  "Tomato ketchup. Condiment; trace calories.");
        d["kielbasa"]                = ("Sausage",                  "Cured pork sausage. Ready to eat; high in fat and protein.");
        d["kisiel"]                  = ("FruitJelly",              "Soft gelatinous dessert. Gentle on the stomach; low calorie.");
        d["likier"]                  = ("Liqueur",                  "Sweet alcoholic liqueur. High sugar and alcohol.");
        d["lody_na_patyku"]          = ("IceLolly",                "Frozen fruit ice on a stick. Will melt quickly outside the freezer.");
        d["maka_ryzowa"]             = ("RiceFlour",               "Finely milled rice flour. Used for cooking and baking.");
        d["marchewki"]               = ("Carrots",                  "Raw carrots. Crunchy, nutritious and long-lasting.");
        d["margaryna"]               = ("Margarine",                "Vegetable-fat spread. Cooking fat and calorie source.");
        d["martwy_karaluch"]         = ("DeadCockroach",           "A dead cockroach. Technically edible protein; morale hits hard.");
        d["martwy_krab"]             = ("DeadCrab",                "A dead crab. Needs cooking. Rich in protein.");
        d["martwy_szczur"]           = ("DeadRat",                 "A dead rat. Risky to eat raw. Cook thoroughly.");
        d["miod"]                    = ("Honey",                    "Natural honey. Excellent energy source and natural preservative.");
        d["mleko_zageszczone"]       = ("CondensedMilk",           "Sweetened condensed milk in a tin. Very calorie-dense.");
        d["musztarda"]               = ("Mustard",                  "Yellow mustard condiment. Minimal nutritional value.");
        d["nuggetsy"]                = ("ChickenNuggets",          "Pre-cooked chicken nuggets. Convenient protein source.");
        d["nutella"]                 = ("Nutella",                  "Hazelnut chocolate spread. High calorie and addictive.");
        d["ogorki"]                  = ("Cucumbers",                "Fresh cucumbers. High water content; good hydration.");
        d["ostry_sos"]               = ("HotSauce",                "Fiery chilli sauce. Condiment only; negligible calories.");
        d["papryka"]                 = ("BellPepper",              "Colourful fresh pepper. Nutritious and hydrating.");
        d["parowki"]                 = ("Frankfurters",             "Pre-cooked sausages in brine. Easy protein on the go.");
        d["protein_shake_gotowy"]    = ("ReadyProteinShake",      "Pre-mixed protein shake. Fast muscle recovery.");
        d["proteinowe_batony"]       = ("ProteinBar",              "Compact high-protein snack bar. Athletes' survival ration.");
        d["racja_wodna"]             = ("WaterRation",             "Military-spec individual water ration. Precisely measured.");
        d["racje_zywnosciowe"]       = ("FoodRationPack",         "Military MRE-style ration. Balanced calories for field conditions.");
        d["rosol"]                   = ("ChickenBroth",            "Clear savoury broth. Warming, hydrating and easy to digest.");
        d["rybiki_cukrowe"]          = ("GummyFish",               "Fish-shaped gelatine sweets. Pure sugar calories.");
        d["ryz_prazony"]             = ("PuffedRice",              "Light puffed rice cereal. Low weight, decent carbohydrates.");
        d["ryz_z_miodem"]            = ("RiceWithHoney",          "Cooked rice drizzled with honey. Sweet energy-dense meal.");
        d["salami"]                  = ("Salami",                   "Dry-cured salami sausage. Long shelf life and high fat content.");
        d["serek_homogenizowany"]    = ("CreamCheese",             "Smooth homogenised cheese. Protein and fat in a soft package.");
        d["shot_mineralow"]          = ("MineralShot",             "Concentrated mineral supplement. Prevents deficiencies.");
        d["tequila"]                 = ("Tequila",                  "Mexican agave spirit. Strong alcohol content.");
        d["wafle_kukurydziane"]      = ("CornWaffles",             "Crispy corn waffles. Light snack, decent carbohydrates.");
        d["wedlina"]                 = ("ColdCuts",                "Assorted cured deli meats. Ready-to-eat protein.");
        d["whey_protein"]            = ("WheyProteinPowder",      "Dry protein powder. Requires water; high protein density.");
        d["whisky"]                  = ("Whisky",                   "Aged grain spirit. Warming effect; impairs coordination.");
        d["wiadro_lodow"]            = ("BucketOfIce",            "A full bucket of ice. Melts fast; temporary cooling or water source.");
        d["wino"]                    = ("Wine",                     "Bottled grape wine. Moderate alcohol; slight morale boost.");
        d["winogrona"]               = ("Grapes",                   "Bunch of fresh grapes. Sweet and hydrating, perishable.");
        d["ziemniaki"]               = ("Potatoes",                 "Raw potatoes. Must be cooked. Filling starchy staple.");
        d["zupki_chinskie"]          = ("InstantNoodles",          "Dry ramen-style noodles. Fast to prepare; salty and filling.");

        // ────────────────────────────────────────────────────
        //  FOOD & DRINK  –  Items\Food drink\default
        // ────────────────────────────────────────────────────
        d["butelka_z_woda_1l"]       = ("WaterBottle1L",          "Standard litre water bottle. Reliable hydration supply.");
        d["chipsy"]                  = ("Crisps",                   "Salty potato crisps. High calorie; poor nutritional value.");
        d["fasola"]                  = ("Beans",                    "Tinned cooked beans. Filling protein and fibre source.");
        d["jajka"]                   = ("Eggs",                     "Fresh chicken eggs. Versatile protein; must be cooked.");
        d["kabanosy"]                = ("KabanosSticks",           "Thin dried pork sausages. Portable high-protein snack.");
        d["kajzerki"]                = ("BreadRolls",              "Small white bread rolls. Fresh but perishable.");
        d["krakersy"]                = ("Crackers",                 "Dry salted crackers. Long shelf life; pairs with anything.");
        d["kukurydza"]               = ("Corn",                     "Tinned or fresh corn kernels. Carbohydrate-rich.");
        d["maslo"]                   = ("Butter",                   "Dairy butter. Rich in fats; good for cooking.");
        d["mleko"]                   = ("Milk",                     "Carton of fresh milk. Nutritious; spoils quickly.");
        d["olej"]                    = ("CookingOil",              "Vegetable cooking oil. Essential for frying; high calorie.");
        d["orzechy"]                 = ("Nuts",                     "Mixed salted nuts. Calorie-dense portable protein source.");
        d["piwo"]                    = ("Beer",                     "Can of lager. Light alcohol; mild morale boost.");
        d["smietana"]                = ("SourCream",               "Thick sour cream. Spoils quickly; good calorie source.");
        d["sol_i_pieprz"]            = ("SaltAndPepper",            "Small shakers of salt and black pepper. Flavour essential.");
        d["suche_jedzenie_dla_zwierzat"] = ("DryPetFood",         "Kibble for cats or dogs. Edible by humans if truly desperate.");
        d["tunczyk"]                 = ("Tuna",                     "Tin of tuna in brine. High protein; long shelf life.");
        d["wodka"]                   = ("Vodka",                    "Clear grain spirit. High alcohol content; morale effect.");
        d["zelki"]                   = ("GummyBears",              "Assorted fruit gummy sweets. Quick sugar boost.");
        d["zupa_pomidorowa"]         = ("TomatoSoup",              "Tinned cream of tomato soup. Warm and comforting.");

        // ────────────────────────────────────────────────────
        //  MEDICINE  –  Items\Medicine
        // ────────────────────────────────────────────────────
        d["alkohol_izopropylowy"]    = ("IsopropylAlcohol",        "70% IPA solution. Sterilises wounds and surfaces.");
        d["antybiotyki"]             = ("Antibiotics",              "Broad-spectrum antibiotic course. Fights bacterial infections.");
        d["balsam_lagodzacy"]        = ("SoothingBalm",            "Topical cooling balm. Relieves minor irritation and rashes.");
        d["chusta_trojkatna"]        = ("TriangularBandage",       "Folded cloth sling. Supports arm fractures and improvised dressings.");
        d["gaza_medyczna"]           = ("MedicalGauze",            "Sterile woven gauze. Primary wound packing material.");
        d["jodyna"]                  = ("Iodine",                   "Antiseptic iodine solution. Cleans and disinfects open wounds.");
        d["koc_termiczny"]           = ("EmergencyThermalBlanket","Reflective mylar sheet. Retains body heat in shock or cold.");
        d["kolnierz_ortopedyczny"]   = ("CervicalCollar",          "Rigid foam collar. Immobilises the neck after injury.");
        d["krople_do_oczu"]          = ("EyeDrops",                "Sterile saline eye drops. Flushes debris and irritants.");
        d["maseczka_chirurgiczna"]   = ("SurgicalMask",            "Disposable face mask. Filters airborne particles and pathogens.");
        d["masc_antyseptyczna"]      = ("AntisepticOintment",      "Topical antibiotic cream. Prevents infection in minor wounds.");
        d["nozyczki_ratownicze"]     = ("TraumaShears",            "Heavy-duty safety scissors. Cuts clothing away in emergencies.");
        d["opaska_uciskowa"]         = ("Tourniquet",               "One-handed windlass tourniquet. Stops life-threatening limb bleeding.");
        d["plyn_do_plukania_ust"]    = ("Mouthwash",                "Antiseptic oral rinse. Reduces infection risk from mouth injuries.");
        d["podkladki_chlodzace"]     = ("CoolingPads",             "Instant cold packs. Reduces swelling and numbs pain on impact.");
        d["rekawiczki_lateksowe"]    = ("LatexGloves",             "Disposable exam gloves. Essential for sterile wound care.");
        d["srodek_na_uspokojenie"]   = ("Sedative",                 "Oral sedative tablet. Reduces anxiety; impairs alertness.");
        d["spray_do_dezynfekcji"]    = ("DisinfectantSpray",       "Broad-spectrum surface disinfectant spray. Kills bacteria and viruses.");
        d["strzykawka"]              = ("Syringe",                  "Sterile single-use syringe. Required for injections or fluid removal.");
        d["szyna_usztywniajaca"]     = ("Splint",                   "Rigid aluminium splint. Immobilises fractured limbs.");
        d["tabletki_nasenne"]        = ("SleepingPills",           "Prescription-strength sleep aid. Dangerous in excess.");
        d["talk_medyczny"]           = ("MedicalTalc",             "Sterile body powder. Prevents friction sores and skin breakdown.");
        d["termometr"]               = ("Thermometer",              "Digital oral thermometer. Detects fever and infection.");
        d["wata_bawelniana"]         = ("CottonWool",              "Soft sterile cotton. Cleaning wounds and applying ointments.");
        d["wegiel_aktywny"]          = ("ActivatedCharcoal",       "Powdered charcoal tablets. Absorbs toxins after ingestion.");
        d["woda_utleniona"]          = ("HydrogenPeroxide",        "3% H₂O₂ solution. Cleans wounds and decontaminates surfaces.");
        d["zel_przeciwbolowy"]       = ("PainReliefGel",          "Topical NSAID gel. Reduces localised pain and inflammation.");

        // ────────────────────────────────────────────────────
        //  MEDICINE  –  Items\Medicine\default
        // ────────────────────────────────────────────────────
        d["bandaz_sterylny"]         = ("SterileBandage",          "Rolled sterile cotton bandage. Wraps and protects dressed wounds.");
        d["bonesow"]                 = ("BoneSaw",                 "Small serrated medical bone saw. Used for field amputations.");
        d["igla_medyczna"]           = ("MedicalNeedle",           "Curved suture needle. Used with surgical thread to close wounds.");
        d["nic_chirurgiczna"]        = ("SurgicalThread",          "Non-absorbable monofilament suture. Closes deep lacerations.");
        d["peseta"]                  = ("Tweezers",                 "Fine-point tweezers. Removes splinters, debris and sutures.");
        d["plastry_opatrunkowe"]     = ("AdhesivePlasters",        "Box of sterile adhesive plasters. Covers minor cuts and grazes.");
        d["srodki_przeciwbolowe"]    = ("Painkillers",              "Over-the-counter analgesics. Reduces pain and mild fever.");
        d["witaminy"]                = ("Vitamins",                 "Multi-vitamin supplement. Prevents nutritional deficiencies over time.");
        d["zestaw_do_szycia_ran"]    = ("WoundSutureKit",         "Complete kit for closing wounds: needle, thread, forceps.");

        // ────────────────────────────────────────────────────
        //  RESOURCES  –  Items\Resources
        // ────────────────────────────────────────────────────
        d["akumulator_samochodowy_jako_magazyn_en"] = ("CarBatteryEnergyStore", "Lead-acid car battery. Stores electrical energy for devices.");
        d["bateria"]                 = ("Battery",                  "AA-size battery. Powers small electronics and torches.");
        d["beton_w_proszku"]         = ("PowderedConcrete",        "Dry concrete mix. Add water to set into hard material.");
        d["blacha_metalowa"]         = ("MetalSheet",              "Flat steel sheet. Used for barricades and construction.");
        d["chusteczki_higieniczne"]  = ("TissuePaper",             "Pack of soft facial tissues. Hygiene and tinder.");
        d["chusteczka_nasaczona_alkoholem"] = ("AlcoholWipe",      "Pre-moistened antiseptic wipe. Quick surface or wound clean.");
        d["cukier_do_przetworow"]    = ("PreservingSugar",         "High-gel sugar for jam making. Also a general sweetener.");
        d["czujnik_ruchu"]           = ("MotionSensor",            "PIR motion detector. Used in security and trap systems.");
        d["czesci_silnika"]          = ("EngineParts",             "Assorted mechanical engine components. Required for vehicle repairs.");
        d["dlugopis"]                = ("BallpointPen",            "Standard biro. Writing tool; also makeshift emergency airway.");
        d["drozdze"]                 = ("Yeast",                    "Dry baker's yeast. Used for baking bread or fermenting alcohol.");
        d["drewno_na_opal"]          = ("Firewood",                 "Split logs for burning. Essential fuel for heat and cooking.");
        d["drut_kolczasty"]          = ("BarbedWire",              "Coiled steel barbed wire. Perimeter defence material.");
        d["dziennik"]                = ("Journal",                  "Blank hardcover journal. Record events, maps and codes.");
        d["farba_biala"]             = ("WhitePaint",              "Tin of white emulsion paint. Marking, camouflage or crafting.");
        d["farba_brazowa"]           = ("BrownPaint",              "Tin of brown paint.");
        d["farba_czarna"]            = ("BlackPaint",              "Tin of black paint.");
        d["farba_czerwona"]          = ("RedPaint",                "Tin of red paint.");
        d["farba_niebieska"]         = ("BluePaint",               "Tin of blue paint.");
        d["farba_pomaranczowa"]      = ("OrangePaint",             "Tin of orange paint.");
        d["farba_szara"]             = ("GreyPaint",               "Tin of grey paint.");
        d["farba_turkusowa"]         = ("TealPaint",               "Tin of teal-coloured paint.");
        d["farba_zielona"]           = ("GreenPaint",              "Tin of green paint.");
        d["farba_zolta"]             = ("YellowPaint",             "Tin of yellow paint.");
        d["galazka"]                 = ("SmallBranch",             "Thin dry branch. Kindling for fires or a makeshift tool handle.");
        d["gazeta"]                  = ("Newspaper",                "Old newspaper. Can be read, used as insulation or as tinder.");
        d["gips"]                    = ("PlasterOfParis",         "Dry plaster powder. Sets hard; used for casts and construction.");
        d["guma_recepturka"]         = ("RubberBand",              "Small elastic band. Lightweight utility fastener.");
        d["gumka_do_mazania"]        = ("Eraser",                   "Standard rubber eraser. Removes pencil marks.");
        d["guzik"]                   = ("Button",                   "Spare clothing button. Minor sewing repair component.");
        d["kartka_papieru"]          = ("SheetOfPaper",           "Blank A4 paper. Writing, drawing, crafting or tinder.");
        d["karty_do_gry"]            = ("PlayingCards",            "Standard 52-card deck. Morale booster; passes time.");
        d["kawalek_materialu"]       = ("FabricScrap",             "Torn piece of cloth. Bandage padding, filter or repair patch.");
        d["kawalek_szkla"]           = ("GlassShard",              "Sharp broken glass. Improvised cutting tool or weapon tip.");
        d["klamka"]                  = ("DoorHandle",              "Metal door handle. Salvaged hardware; minor crafting component.");
        d["klej_do_drewna"]          = ("WoodGlue",                "PVA wood adhesive. Bonds timber and porous materials.");
        d["komiks"]                  = ("ComicBook",               "Colourful comic book. Morale item; can be used as tinder.");
        d["kondensator"]             = ("Capacitor",                "Electronic capacitor. Component for electrical repairs.");
        d["kora_drzewna"]            = ("TreeBark",                "Peeled bark section. Tinder, improvised container or cordage.");
        d["kostki_do_gry"]           = ("Dice",                     "Set of six-sided dice. Morale item; pass time between shifts.");
        d["kreda"]                   = ("Chalk",                    "White chalk stick. Marking surfaces and leaving messages.");
        d["kreda_czerwona"]          = ("RedChalk",                "Red chalk stick.");
        d["kreda_niebieska"]         = ("BlueChalk",               "Blue chalk stick.");
        d["kreda_zielona"]           = ("GreenChalk",              "Green chalk stick.");
        d["kreda_zolta"]             = ("YellowChalk",             "Yellow chalk stick.");
        d["kufel"]                   = ("BeerMug",                 "Heavy glass beer mug. Container or blunt improvised weapon.");
        d["lina_z_przescieradel"]    = ("SheetRope",               "Knotted bedsheet rope. Useful for climbing or securing loads.");
        d["magazyn"]                 = ("Magazine",                 "Printed magazine. Morale item or emergency tinder.");
        d["mala_blacha_metalowa"]    = ("SmallMetalSheet",        "Small flat piece of sheet metal. Lightweight construction part.");
        d["mydlo"]                   = ("Soap",                     "Bar of soap. Essential hygiene; also lubricant for hinges.");
        d["nadajnik_radiowy"]        = ("RadioTransmitter",        "Handheld radio transmitter. Sends signals over short range.");
        d["nakretka_do_sloika"]      = ("JarLid",                  "Metal screw-top jar lid. Sealing preserved food containers.");
        d["nasiona_brokula"]         = ("BroccoliSeeds",           "Packet of broccoli seeds. Plant for a long-term food source.");
        d["nasiona_kapusty"]         = ("CabbageSeeds",            "Packet of cabbage seeds.");
        d["nasiona_marchewki"]       = ("CarrotSeeds",             "Packet of carrot seeds.");
        d["nasiona_pomidora"]        = ("TomatoSeeds",             "Packet of tomato seeds.");
        d["nasiona_rzodkiewki"]      = ("RadishSeeds",             "Packet of radish seeds. Fast-growing; ready in 3–4 weeks.");
        d["nasiona_truskawki"]       = ("StrawberrySeeds",         "Packet of strawberry seeds.");
        d["nasiona_ziemniaka"]       = ("PotatoSeed",              "Seed potato. Plant in soil for a reliable crop.");
        d["nawoz_npk"]               = ("NPKFertiliser",           "Balanced nitrogen-phosphorus-potassium fertiliser. Boosts crop yield.");
        d["odbiornik_radiowy"]       = ("RadioReceiver",           "Portable radio receiver. Picks up broadcasts and emergency signals.");
        d["olowek"]                  = ("Pencil",                   "Graphite pencil. Writing and marking tool.");
        d["papier_toaletowy"]        = ("ToiletPaper",             "Roll of toilet paper. Hygiene essential; also tinder.");
        d["papierosy"]               = ("Cigarettes",               "Pack of cigarettes. Trade currency and minor stress relief.");
        d["patyk"]                   = ("Stick",                    "Straight dry stick. Primitive tool component or fire-starting kit.");
        d["pilot_do_zdalnego_sterowania"] = ("RemoteControl",      "TV remote control. Contains batteries; circuits can be salvaged.");
        d["plytka_drukowana"]        = ("CircuitBoard",            "Electronic PCB. Core component for advanced electrical crafting.");
        d["podarte_przescieradla"]   = ("TornBedsheets",           "Strips of ripped bedsheet. Bandage material, rope or padding.");
        d["przewod_elektryczny"]     = ("ElectricWire",            "Length of insulated copper wire. Essential for electrical work.");
        d["proszek_do_pieczenia"]    = ("BakingPowder",            "Leavening agent for baking. Also used in minor cleaning.");
        d["pudelko_na_naboje_puste"] = ("EmptyAmmoBox",           "Metal ammo storage box. Durable waterproof container.");
        d["pudelko_z_bizuteria_surowiec"] = ("JewelleryBoxRaw", "Small box of loose metal pieces and gems. Crafting material.");
        d["pudelko_zapalek"]         = ("BoxOfMatches",           "Strike-anywhere matches. Essential fire-starting tool.");
        d["pusta_butelka_plastikowa"] = ("EmptyPlasticBottle",    "Empty PET bottle. Water storage, filter or flotation device.");
        d["puste_pudelko_po_zapalkach"] = ("EmptyMatchbox",        "Spent matchbox. Small storage for tiny items.");
        d["pusty_worek_na_piasek"]   = ("EmptySandbag",            "Burlap sandbag. Fill with sand or earth for fortifications.");
        d["puszka_aluminiowa"]       = ("AluminiumCan",            "Empty drink can. Metal crafting stock or improvised cup.");
        d["recznik_papierowy"]       = ("PaperTowel",              "Roll of absorbent paper towel. Cleaning and hygiene.");
        d["resystor"]                = ("Resistor",                 "Small electronic resistor. Component for electrical circuits.");
        d["rozpalka"]                = ("Firelighter",              "Solid fuel firelighter block. Starts fires quickly even when damp.");
        d["sol_do_konserwacji"]      = ("PreservingSalt",          "Coarse curing salt. Preserves meat and vegetables.");
        d["sruba_do_kol"]            = ("WheelBolt",               "Threaded wheel fastener. Vehicle maintenance component.");
        d["szklanka"]                = ("Glass",                    "Drinking glass. Container; shatters into improvised blades.");
        d["szpilka"]                 = ("Pin",                      "Metal dressmaking pin. Tiny fastener; improvised lock pick.");
        d["szpula_drutu_miedzianego"] = ("CopperWireSpool",       "Reel of thin copper wire. Electrical wiring and snare making.");
        d["tasma_klejaca"]           = ("AdhesiveTape",            "Roll of clear sticky tape. Light-duty bonding and sealing.");
        d["tranzystor"]              = ("Transistor",               "Electronic transistor. Amplification component for circuits.");
        d["wata"]                    = ("CottonWoolRaw",        "Raw cotton wool. Padding, tinder or wound care.");
        d["wkret"]                   = ("WoodScrew",               "Self-tapping wood screw. Construction fastener.");
        d["wloczka"]                 = ("Yarn",                     "Ball of knitting yarn. Textile crafting and repairs.");
        d["worek_na_smieci"]         = ("BinBag",                  "Large heavy-duty bin liner. Waterproof cover or improvised poncho.");
        d["worek_piasku"]            = ("Sandbag",                  "Pre-filled sandbag. Place to reinforce barricades and walls.");
        d["worek_w_zwiru"]           = ("GravelBag",               "Bag filled with gravel. Heavier than sand; ballast or fortification.");
        d["worek_ziemi"]             = ("SoilBag",                 "Bag of loose earth. Gardening or improvised fortification.");
        d["worki_po_nawozie"]        = ("EmptyFertiliserBags",    "Empty plastic fertiliser bags. Waterproof sheeting material.");
        d["wybielacz"]               = ("Bleach",                   "Sodium hypochlorite solution. Disinfectant; purifies water in drops.");
        d["wzmacniacz"]              = ("Amplifier",                "Electronic signal amplifier. Boosts radio or audio signals.");
        d["zarowka"]                 = ("LightBulb",               "Standard incandescent bulb. Illumination; glass for improvised items.");
        d["zarowka_czerwona"]        = ("RedLightBulb",           "Red-tinted incandescent bulb. Signalling or mood lighting.");
        d["zarowka_niebieska"]       = ("BlueLightBulb",          "Blue-tinted bulb.");
        d["zarowka_zielona"]         = ("GreenLightBulb",         "Green-tinted bulb.");
        d["zatyczki_do_uszu"]        = ("EarPlugs",                "Foam ear plugs. Hearing protection in noisy environments.");
        d["zawiasy_do_drzwi"]        = ("DoorHinges",              "Metal door hinges. Construction and barricade hardware.");
        d["zegarek_cyfrowy_do_demontazu"] = ("DigitalWatchDisassemble", "Broken digital watch. Yields circuit and battery components.");
        d["zegarek_mechaniczny_do_demontazu"] = ("MechanicalWatchDisassemble", "Old mechanical watch. Yields springs, gears and glass.");
        d["zeszyt"]                  = ("Notebook",                 "Spiral-bound notebook. Notes, maps and resource tracking.");
        d["zetony_do_gry"]           = ("GameTokens",              "Plastic game counters. Morale; can mark map locations.");

        // ────────────────────────────────────────────────────
        //  RESOURCES  –  Items\Resources\default
        // ────────────────────────────────────────────────────
        d["agrafka"]                 = ("SafetyPin",               "Small steel safety pin. Clothing repair and improvised clasp.");
        d["brudne_szmaty"]           = ("DirtyRags",               "Grimy cloth scraps. Cleaning, padding or fuel soaked in oil.");
        d["deska"]                   = ("Plank",                    "Flat wooden plank. Primary construction material.");
        d["drut"]                    = ("Wire",                     "Length of steel wire. Binding, snares and structural ties.");
        d["gwozdz"]                  = ("Nail",                     "Iron nail. Structural fastener for wood construction.");
        d["igla_do_szycia"]          = ("SewingNeedle",            "Fine steel hand-sewing needle. Clothing and wound sutures.");
        d["kamien"]                  = ("Stone",                    "Smooth stone. Thrown projectile or primitive crafting material.");
        d["klej_biurowy"]            = ("OfficeGlue",              "Liquid PVA glue stick. Light-duty bonding.");
        d["lina"]                    = ("Rope",                     "Strong braided rope. Climbing, binding and construction.");
        d["nici"]                    = ("Thread",                   "Reel of sewing thread. Clothing repairs and sutures.");
        d["ostry_kamien"]            = ("SharpStone",              "Flint-edged stone. Primitive cutting tool or fire starter.");
        d["paski_jeansu"]            = ("DenimStrips",             "Cut strips of denim fabric. Binding, patches or improvised armour.");
        d["paski_skory"]             = ("LeatherStrips",           "Thin cut leather strips. Lashing, armour straps and repairs.");
        d["pudelko_na_gwozdzie"]     = ("NailBox",                 "Small box containing assorted nails. Bulk construction fasteners.");
        d["pusta_butelka_szklana"]   = ("EmptyGlassBottle",       "Empty glass bottle. Molotov container or water storage.");
        d["rura_metalowa"]           = ("MetalPipe",               "Length of steel pipe. Construction element or melee weapon.");
        d["sloik"]                   = ("GlassJar",                "Sealed glass jar. Food preservation or liquid storage.");
        d["szmurek"]                 = ("Twine",                    "Thin natural fibre cord. Light-duty binding and crafting.");
        d["tasma_naprawcza_duct_tape"] = ("DuctTape",              "Heavy-duty fabric adhesive tape. The ultimate repair tool.");
        d["zlom_elektroniczny"]      = ("ElectronicScrap",         "Mixed salvaged electronic parts. Circuit crafting component.");
        d["zlom_metalowy"]           = ("MetalScrap",              "Assorted metal pieces. Smelted or used in improvised construction.");

        return d;
    }
}
#endif
