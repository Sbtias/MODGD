# MODGD Platformer Save

Geode mod for Geometry Dash 2.2081 that saves and restores Platformer level progress.

## Features

- Saves platformer checkpoint state.
- Restores the saved run when reopening the level.
- Stores saves separately per level.
- Offers New Save, Load Save, No Save, and Delete.
- Saves again when leaving the level.
- Optional removal of the save after completing the level.

## Build

This project targets Windows / Geometry Dash 2.2081 and Geode 5.7.1.

Requirements:
- Geode SDK
- CMake 3.29+
- C++23 compiler
- sabe.persistenceapi >= v1.2.0

The implementation is a modified version of Platformer Progression Save by Frxme, released under GPL-3.0-or-later.

Original project:
https://github.com/Framepersecond/Geode-Platformer-save
