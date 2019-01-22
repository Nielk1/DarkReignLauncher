using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DarkHook
{
    public enum TACTICS_RUNCODE
    {
        RUNSHELL,                     // run the new shell
        RUNMULTIMENU,                 // run the multi player menu
        RUNGAME,                      // run the game
        RUNNINGGAME,                  // game in progress
        RUNWITHNOPROCESSING,          // eg. game won, waiting for quit
        RUNEDIT,                      // run the mapeditor
        QUITTODOS,                    // quit to dos
        LOADGAME,                     // load a saved game
    }
    public enum GAME_TYPE
    {
        Fixed,
        Single,
        Multi,
        Campaign,
    }
    public enum STARTING_UNITS
    {
        Normal,
        ThreeRigs,
    }

    public enum TEAM_TYPE
    {
        TT_DISABLED = 0,
        TT_HUMAN = 1,
        TT_COMPUTER = 2,
    }

    public enum TEAM_RELATION
    {
        TR_THIS     = 0x00,
        TR_ENEMY    = 0x01,
        TR_NEUTRAL  = 0x02,
        TR_ALLY     = 0x04,
        TR_ALL      = TR_ENEMY | TR_NEUTRAL | TR_ALLY
    }

    public enum TEAM_SIDE
    {
        TS_DEFAULT = -1,
        TS_FREEDOM_GUARD = 0,
        TS_IMPERIUM = 1,
        TS_CIVILIAN = 2,
        TS_TOGRAN = 3,
        TS_XENITE = 4,
        TS_SHADOWHAND = 5,
        TS_MAX,
    }

    public class DarkReignInterface
    {
        const UInt32 tinfo = 0x0074D718;
        const UInt32 tinfo__runcode = 0x00000EF4;
        const UInt32 tinfo__gametype = 0x00000FFC;
        const UInt32 tinfo__instant_action = 0x00001B7C;
        const UInt32 tinfo__scenarioname = 0x00001000;
        const UInt32 tinfo__scenariodir = 0x00001104;
        const UInt32 tinfo__scenarioscn = 0x00001208;
        const UInt32 tinfo__scenariomap = 0x0000130C;
        const UInt32 tinfo__scenariomm = 0x00001410;
        const UInt32 tinfo__terrainname = 0x00001514;
        const UInt32 tinfo__brightness = 0x00001A56;
        const UInt32 tinfo__paused = 0x00001A57;
        const UInt32 tinfo__ineditor = 0x00001A58;
        const UInt32 tinfo__disable_foundations = 0x00001A59;
        const UInt32 tinfo__disable_fog = 0x00001A5A;
        const UInt32 tinfo__disable_black = 0x00001A5B;
        const UInt32 tinfo__disable_giving = 0x00001A5C;
        const UInt32 tinfo__disable_viewally = 0x00001A5D;
        const UInt32 tinfo__disable_alliances = 0x00001A5E;
        const UInt32 tinfo__starting_units = 0x00001A5F;
        const UInt32 tinfo__techlevel = 0x00001B6C;
        const UInt32 tinfo__gamespd = 0x00001B74;
        const UInt32 myteam = 0x0074D664;

        const UInt32 teamdata = 0x007611A0;
        const UInt32 teamdata__type = 0x00000000;
        const UInt32 teamdata__initial_type = 0x00000004;
        const UInt32 teamdata__playername = 0x00000008;
        const UInt32 teamdata__group = 0x00000018;
        const UInt32 teamdata__minimap = 0x0000001C;
        const UInt32 teamdata__relation = 0x00000020;
        const UInt32 teamdata__lineofsight = 0x00000044;
        const UInt32 teamdata__seeresource = 0x00000068;
        const UInt32 teamdata__credits = 0x0000008C;
        const UInt32 teamdata__start_credits = 0x00000090;
        const UInt32 teamdata__resource = 0x00000094;
        const UInt32 teamdata__cpu = 0x00000094;
        const UInt32 teamdata__unitcount = 0x00000108;
        const UInt32 teamdata__side = 0x0000010C;
        const UInt32 teamdata__non_default = 0x00000110;
        const UInt32 teamdata__colour = 0x00000114;
        const UInt32 teamdata__palindex = 0x00000118;
        const UInt32 teamdata__killemall = 0x0000011C;
        const UInt32 teamdata__stats = 0x00000210;
        const UInt32 teamdata__UNK1 = 0x0000023C;
        const UInt32 teamdata__UNK2 = 0x00000240;
        const UInt32 teamdata__UNK3 = 0x00000244;
        const UInt32 teamdata__UNK4 = 0x00000248;
        const UInt32 teamdata__startpos__x = 0x0000024C;
        const UInt32 teamdata__startpos__y = 0x0000024C + 0x00000004;
        const UInt32 teamdata__unitdef = 0x00000254;
        const UInt32 teamdata__UNK5 = 0x00000260;
        const UInt32 teamdata__UNK6 = 0x00000264;
        const UInt32 teamdata__UNK7 = 0x00000268;
        const UInt32 teamdata__UNK8 = 0x0000026C;
        const UInt32 teamdata__UNK9 = 0x00000270;
        const UInt32 teamdata__size = 0x00000274;

        const UInt32 teamdata__stats__kills_units = 0x00000000;
        const UInt32 teamdata__stats__kills_buildings = 0x00000004;
        const UInt32 teamdata__stats__losses_units = 0x00000008;
        const UInt32 teamdata__stats__losses_buildings = 0x0000000C;
        const UInt32 teamdata__stats__made_units = 0x00000010;
        const UInt32 teamdata__stats__made_buildings = 0x00000014;
        const UInt32 teamdata__stats__collected = 0x00000018;

        const UInt32 teamdata__teamresource__suppply = 0x00000000;
        const UInt32 teamdata__teamresource__usage = 0x00000004;
        const UInt32 teamdata__teamresource__percent = 0x00000008;
        const UInt32 teamdata__teamresource__size = 0x0000000C;

        const UInt32 teamdata__cpu__load = 0x00000000;
        const UInt32 teamdata__cpu__maxload = 0x00000004;
        const UInt32 teamdata__cpu__damage = 0x00000008;
        const UInt32 teamdata__cpu__percent = 0x0000000C;
        const UInt32 teamdata__cpu__size = 0x00000010;

        const UInt32 teamdata__unitdef__autonomy = 0x00000000;
        const UInt32 teamdata__unitdef__tenacity = 0x00000004;
        const UInt32 teamdata__unitdef__selfpres = 0x00000008;

        public TACTICS_RUNCODE Runcode { get { return (TACTICS_RUNCODE)Memory.ReadMemoryUInt32(pid, new IntPtr(tinfo + tinfo__runcode)); } }
        public GAME_TYPE GameType { get { return (GAME_TYPE)Memory.ReadMemoryUInt32(pid, new IntPtr(tinfo + tinfo__gametype)); } }
        public bool InstantAction { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(tinfo + tinfo__instant_action)) > 0; } }
        public string ScenarioName { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__scenarioname), 260); } }
        public string ScenarioDir { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__scenariodir), 260); } }
        public string ScenarioScn { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__scenarioscn), 260); } }
        public string ScenarioMap { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__scenariomap), 260); } }
        public string ScenarioMm { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__scenariomm), 260); } }
        public string TerrainName { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(tinfo + tinfo__terrainname), 13); } }
        public byte Brightness { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__brightness)); } }
        public bool Paused { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__paused)) > 0; } }
        public bool InEditor { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__ineditor)) > 0; } }
        public bool DisableFoundations { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_foundations)) > 0; } }
        public bool DisableFog { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_fog)) > 0; } }
        public bool DisableBlack { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_black)) > 0; } }
        public bool DisableGiving { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_giving)) > 0; } }
        public bool DisableViewally { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_viewally)) > 0; } }
        public bool DisableAlliances { get { return Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__disable_alliances)) > 0; } }
        public STARTING_UNITS StartingUnits { get { return (STARTING_UNITS)Memory.ReadMemoryByte(pid, new IntPtr(tinfo + tinfo__starting_units)); } }
        public UInt32 TechLevel { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(tinfo + tinfo__techlevel)); } }
        public UInt32 GameSpeed { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(tinfo + tinfo__gamespd)); } }
        public Int32 MyTeam { get { return Memory.ReadMemoryInt32(pid, new IntPtr(myteam)); } }

        public TEAM_TYPE Team_Type { get { return (TEAM_TYPE)Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__type)); } }
        public TEAM_TYPE Team_InitialType { get { return (TEAM_TYPE)Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__initial_type)); } }
        public string Team_PlayerName { get { return Memory.ReadMemoryAsciiString(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__playername), 16); } }
        public Int32 Team_Group { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__group)); } }
        public Int32 Team_MiniMap { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__minimap)); } }
        public TEAM_RELATION[] Team_Relation { get { return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.Select(dr => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__relation + dr * sizeof(int)))).Select(dr => (TEAM_RELATION)dr).ToArray(); } }
        public Int32[] Team_LineOfSight { get { return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.Select(dr => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__lineofsight + dr * sizeof(int)))).Select(dr => dr).ToArray(); } }
        public Int32[] Team_SeeResource { get { return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }.Select(dr => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__seeresource + dr * sizeof(int)))).Select(dr => dr).ToArray(); } }
        public Int32 Team_Credits { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__credits)); } }
        public Int32 Team_StartCredits { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__start_credits)); } }
        public Int32[] Team_Resource_Suppply { get { return new int[] { 0, 1, 2, 3, 4 }.Select(idx => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__resource + (idx * teamdata__teamresource__size) + teamdata__teamresource__suppply))).ToArray(); } }
        public Int32[] Team_Resource_Usage { get { return new int[] { 0, 1, 2, 3, 4 }.Select(idx => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__resource + (idx * teamdata__teamresource__size) + teamdata__teamresource__usage))).ToArray(); } }
        public Int32[] Team_Resource_Percent { get { return new int[] { 0, 1, 2, 3, 4 }.Select(idx => Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__resource + (idx * teamdata__teamresource__size) + teamdata__teamresource__percent))).ToArray(); } }
        public Int32 Team_CPU_Load { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__cpu + teamdata__cpu__load)); } }
        public Int32 Team_CPU_MaxLoad { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__cpu + teamdata__cpu__maxload)); } }
        public Int32 Team_CPU_Damage { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__cpu + teamdata__cpu__damage)); } }
        public Int32 Team_CPU_Percent { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__cpu + teamdata__cpu__percent)); } }
        public Int32 Team_UnitCount { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__unitcount)); } }
        public TEAM_SIDE Team_Side { get { return (TEAM_SIDE)Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__side)); } }
        public bool Team_NonDefault { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__non_default)) > 0; } }
        public Int32 Team_Color { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__colour)); } }
        public Int32 Team_PalIndex { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__palindex)); } }
        public bool Team_KillEmAll { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__killemall)) > 0; } }
        public Int32 Team_Stats_KillsUnits { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__kills_units)); } }
        public Int32 Team_Stats_KillsBuildings { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__kills_buildings)); } }
        public Int32 Team_Stats_LossesUnits { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__losses_units)); } }
        public Int32 Team_Stats_LossesBuildings { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__losses_buildings)); } }
        public Int32 Team_Stats_MadeUnits { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__made_units)); } }
        public Int32 Team_Stats_MadeBuildings { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__made_buildings)); } }
        public Int32 Team_Stats_Collected { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__stats + teamdata__stats__collected)); } }

        public UInt32 Team_UNK1 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK1)); } }
        public UInt32 Team_UNK2 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK2)); } }
        public UInt32 Team_UNK3 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK3)); } }
        public UInt32 Team_UNK4 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK4)); } }

        public Int32 Team_StartPos_X { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__startpos__x)); } }
        public Int32 Team_StartPos_Y { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__startpos__y)); } }

        public Int32 Team_UnitDef_Autonomy { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__unitdef + teamdata__unitdef__autonomy)); } }
        public Int32 Team_UnitDef_Tenacit { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__unitdef + teamdata__unitdef__tenacity)); } }
        public Int32 Team_UnitDef_SelfPres { get { return Memory.ReadMemoryInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__unitdef + teamdata__unitdef__selfpres)); } }

        public UInt32 Team_UNK5 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK5)); } }
        public UInt32 Team_UNK6 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK6)); } }
        public UInt32 Team_UNK7 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK7)); } }
        public UInt32 Team_UNK8 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK8)); } }
        public UInt32 Team_UNK9 { get { return Memory.ReadMemoryUInt32(pid, new IntPtr(teamdata + (MyTeam * teamdata__size) + teamdata__UNK9)); } }

        private IntPtr pid;
        //private Process proc;
        public DarkReignInterface(Process proc)
        {
            pid = Kernel32.OpenProcess(0x1F0FFF, false, proc.Id);
            //this.proc = proc;
        }

        public DarkReignInterface(int procId)
        {
            pid = Kernel32.OpenProcess(0x1F0FFF, false, procId);
        }
    }
}
