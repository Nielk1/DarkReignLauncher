//
// shell_cfg.h
//
// Configuration for Shell (fonts, etc.)

// Names of fonts in the compressed shell assets to use for different things
#define FONT_0_NAME "font12"	// subtitles for outer shell
#define FONT_1_NAME "font14"	// titles for inner shell
#define FONT_2_NAME "font14n"	// buttons for inner shell (normal)
#define FONT_3_NAME "font14o"	// buttons for inner shell (mouseover)
#define FONT_4_NAME "font14s"	// buttons for inner shell (selected)
#define FONT_5_NAME "font16"
#define FONT_6_NAME "font14r"	// red titles for inner shell
#define FONT_7_NAME "font16r"
#define FONT_8_NAME "font12n"
#define FONT_9_NAME "font12n"
#define FONT_10_NAME "font12n"	// buttons for outer shell (normal)
#define FONT_11_NAME "font12o"	// buttons for outer shell (mouseover)
#define FONT_12_NAME "font12s"	// buttons for outer shell (selected)
#define FONT_13_NAME "font12"	// title for outer shell

#define FONT_14_NAME "font12ne"	        // briefing screen title
#define FONT_15_NAME "font14ne"	        // briefing screen title
#define FONT_16_NAME "font14oe"	        // briefing screen title
#define FONT_17_NAME "font14se"	        // briefing screen title


// Set a default button width
#define BTN_DEFAULT_WIDTH 100

// Fonts to use for the credits
#define CREDITS_TITLE_FONT    6
#define CREDITS_NAME_FONT     1
#define CREDITS_SUBTEXT_FONT  8

// Main Menu Button positions
#define BTN_MAIN_SINGLE_X 240
#define BTN_MAIN_SINGLE_Y 86
#define BTN_MAIN_MULTI_X 240
#define BTN_MAIN_MULTI_Y 136
#define BTN_MAIN_INSTANT_X 240
#define BTN_MAIN_INSTANT_Y 188
#define BTN_MAIN_CONSTRUCTION_X 240
#define BTN_MAIN_CONSTRUCTION_Y 238
#define BTN_MAIN_REPLAY_X 240
#define BTN_MAIN_REPLAY_Y 291
#define BTN_MAIN_CREDITS_X 240
#define BTN_MAIN_CREDITS_Y 342
#define BTN_MAIN_QUIT_X 240
#define BTN_MAIN_QUIT_Y 411
#define BTN_MAIN_BRIGHT_X 434
#define BTN_MAIN_BRIGHT_Y 411
#define BTN_MAIN_DARK_X 44
#define BTN_MAIN_DARK_Y 411

// Single player menu button positions
#define BTN_SINGLE_EXPANSION_X  240
#define BTN_SINGLE_EXPANSION_Y  800
#define BTN_SINGLE_START_X      240
#define BTN_SINGLE_START_Y      136
#define BTN_SINGLE_LOAD_X       240
#define BTN_SINGLE_LOAD_Y       238
#define BTN_SINGLE_CUSTOM_X     240
#define BTN_SINGLE_CUSTOM_Y     288
#define BTN_SINGLE_PREVIOUS_X   240
#define BTN_SINGLE_PREVIOUS_Y   412
#define SINGLE_DRAW_CD_X        138
#define SINGLE_DRAW_CD_Y        30
#define SINGLE_DRAW_CD_W        360
#define SINGLE_DRAW_CD_H        40
#define MSG_SINGLE_DRAW_CD_X    161
#define MSG_SINGLE_DRAW_CD_Y    36

// Custom mission menu buttons
#define BTN_CUSTOM_LOAD_X 270
#define BTN_CUSTOM_LOAD_Y 411
#define BTN_CUSTOM_PREVIOUS_X 30
#define BTN_CUSTOM_PREVIOUS_Y 415
#define BTN_CUSTOM_SIDE_X 515
#define BTN_CUSTOM_SIDE_Y 141
#define BTN_CUSTOM_SIDE_DYNAMIC_Y 163
#define BTN_CUSTOM_SIZE_X 555
#define BTN_CUSTOM_SIZE_Y 84
#define BTN_CUSTOM_SIZE_DYNAMIC_Y 103
#define BTN_CUSTOM_ENEMIES_X 470
#define BTN_CUSTOM_ENEMIES_Y 84
#define BTN_CUSTOM_ENEMIES_DYNAMIC_Y 103
#define BTN_CUSTOM_DELETE_X 145
#define BTN_CUSTOM_DELETE_Y 415

// Stats menu buttons
#define BTN_STATS_CONTINUE_X 240
#define BTN_STATS_CONTINUE_Y 411
#define BTN_STATS_MAIN_X 480
#define BTN_STATS_MAIN_Y 415

// Load menu buttons
#define BTN_LOAD_BACK_X 30
#define BTN_LOAD_BACK_Y 415
#define BTN_LOAD_LAUNCH_X 270
#define BTN_LOAD_LAUNCH_Y 411

// Load menu saved games box
#define BOX_LOAD_SAVED_GAMES_LEFT 60
#define BOX_LOAD_SAVED_GAMES_TOP 102
#define BOX_LOAD_SAVED_GAMES_WIDTH 346
#define BOX_LOAD_SAVED_GAMES_HEIGHT 281

// Load menu text
#define TEXT_LOAD_TITLE_X 320
#define TEXT_LOAD_TITLE_Y 46
#define TEXT_LOAD_LOCATION_X 515
#define TEXT_LOAD_LOCATION_Y 82
#define TEXT_LOAD_LOCATION_DYNAMIC_Y 104
#define TEXT_LOAD_PROGRESSION_X 515
#define TEXT_LOAD_PROGRESSION_Y 141
#define TEXT_LOAD_PROGRESSION_DYNAMIC_Y 163

// Credits menu buttons
#define BTN_CREDITS_PREVIOUS_X 0
#define BTN_CREDITS_PREVIOUS_Y 415
#define BTN_CREDITS_SLOWER_X 73
#define BTN_CREDITS_SLOWER_Y 362
#define BTN_CREDITS_FASTER_X 524
#define BTN_CREDITS_FASTER_Y 363
#define BTN_CREDITS_FASTER_W 60
#define BTN_CREDITS_FASTER_H 40
#define CREDITS_AUS_CENTER 420
#define CREDITS_USA_CENTER 220
#define CREDITS_MAX_SCROLL 30

// Mission menu buttons
#define BTN_MISSION_COEFFICIENT 157
#define BTN_MISSION_ADVANCED_X 502
#define BTN_MISSION_ADVANCED_Y 77
#define BTN_MISSION_ADVANCED_W 114
#define BTN_MISSION_ADVANCED_H 50
#define BTN_MISSION_BASIC_X 22
#define BTN_MISSION_BASIC_Y 77
#define BTN_MISSION_BASIC_W 114
#define BTN_MISSION_BASIC_H 50
#define BTN_MISSION_DOWN_ARROW_X 300
#define BTN_MISSION_DOWN_ARROW_Y 453

// Archive buttons
#define BOX_ARCHIVE_LEFT 114
#define BOX_ARCHIVE_TOP 60
#define BOX_ARCHIVE_WIDTH 420
#define BOX_ARCHIVE_HEIGHT 310
#define BTN_ARCHIVE_UP_ONE_X 205
#define BTN_ARCHIVE_UP_ONE_Y (BOX_ARCHIVE_TOP + BOX_ARCHIVE_HEIGHT + 6)
#define BTN_ARCHIVE_UP_ONE_W 236
#define BTN_ARCHIVE_UP_ONE_H 36

// Options Edit Box
#define EDIT_OPTIONS_SAVE_NAME_X 200
#define EDIT_OPTIONS_SAVE_NAME_Y 65
#define EDIT_OPTIONS_SAVE_NAME_W 210
#define EDIT_OPTIONS_SAVE_BOX_X 160
#define EDIT_OPTIONS_SAVE_BOX_Y 55
#define EDIT_OPTIONS_SAVE_BOX_W 320
#define EDIT_OPTIONS_SAVE_BOX_H 40
#define TEXT_OPTIONS_PROG_Y 176
#define TEXT_OPTIONS_PROG_DATA_Y 203
#define TEXT_OPTIONS_STATS_X 455










