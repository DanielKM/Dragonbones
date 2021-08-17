# RTS Engine v2.0 BETA Repository

v2.0 is now close to being released but there are a couple of features that still need to be implemented first and most importantly, more testing is required to make sure that the current systems are robust enough.

If you are planning to customize the RTS Engine 2.0 through your own custom components by tapping into the RTS Engine API, please be aware that the API is subject to change. Adequate informatio will be provided in case a major change occurs for which you need to update your custom components.

Bullet points of what changes to expect can be found here: https://trello.com/b/pUaQ2K7r/rts-engine-20-beta

## Motivation
  
  I am taking my time working on this major upgrade mainly due to the fact that I am trying to build a framework and not just an asset that you can reskin. The goal is to open up customization through a comprehensible API and allow the community to submit their modifications and mods built ontop of the framework. I have came to realize that this is the best way to allow everyone to really customize the asset to their liking and the best way to proceed about it is to have a feature-rich framework instead of attempting to build all possible features that a RTS game might include by myself.

## Installation

Clone the repository and open the project using Unity 2019.4.12f or a higher version.

## Structure

The project contains one main folder under the name of *RTS Engine*. This folder contains 4 subfolders:
  * *Core*: Includes the core components and assets of the RTS Engine. This folder is required to have a functional RTS Engine game.
  * *Legacy*: Includes components that will be deprecated from RTS Engine 1. It also includes demo assets that have yet to be converted to 2.0. Please do not use any components that are still in this folder as they will be heavily modified or completely removed at one point. This folder will be completely gone by the time 2.0 is released.
  * *Demo*: Includes assets for the new demo game. This folder is removable and no issue would arise with the asset.
  * *Extensions*: Includes optional extensions that can be plugged into the RTS Engine. This includes the singleplayer lobby system, the whole multiplayer system and more.
  
The *Mirror* folder contains the Mirror asset, the main networking HLAPI that the RTS Engine supports.
  
 ## Workflow
 
 * It is recommended that you work on a separate branch and that you regularly check the *master* branch on this repo and merge it into your own branch. 
 * Only manipulate files that are not inside the *RTS Engine* and *Mirror* folders as I will be only modifying files that are under those paths. Therefore, keep everything you work on separately from the asset and organize the structure of your assets as you wish.
 * If you would like to add something to the master branch, please work on your changes on a separate branch, then create a pull request and I'll review it and get it merged into the master branch if it proves to be useful.
 
 ## Feedback
 
For now, please use the RTS Engine 2.0 BETA specific channels in the Discord server (invite link: https://discord.gg/BsdQNKY)
