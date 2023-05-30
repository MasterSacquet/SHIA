# Mike the Pedagogical Agent

Welcome to the official repository of Mike The pedagogical agent.

<img src="./img/agent.gif" width="20%"/>

This is a Unity3D project which was originally created for a CentraleSupelec Lecture on Artificial Intelligence and Social Sciences.

This project contains the foundation of a Socially Interactive Agent or Embodied Conversational Agent.
It is greatly inspired by other projects such as Greta, Marc and the Virtual Human Toolkit.
It was not really designed to fully reproduce the capacity of a SIA, but to offer an easy way for students to discover how an agent can work.

# Installation

You can clone the current project using your favorite Git Client or you can download it as a zip archive and extract it on your drive.

Mike the PA should work on Linux, Windows and MacOS versions of Unity3D starting from version 2021.3.
- On MacOSX, it is possible to get a compiling error saying it can not find the Newtonsoft Json package. Just copying the dll found in Packages/Newtonsoft.Json.13.0.1/lib/\*/ in the Assets folder should fix the issue.

As a Unity3D project, you need to add the project in your project's list using UnityHub "Add Project" functionality.

# Usage

The project should open on a blank scene. Open the SampleScene to load the default scene. If you are familiar with Unity3D, you should get around quite easily.

The scene only has a fake background, a Camera, some lighting and our agent. The agent is a GameObject equipped with custom scripts in order to allow him to :
- Run a pre-scripted interactive dialog thanks to UI Buttons.  
- Follow an object with its gaze
- Perform some pre-rendered animations (downloaded from Mixamo)
- Do basic lip animation mixed with facial expressions

The project is quite light and straightforward. It does not compete with other exising agent plaforms such as [Greta](https://github.com/isir/greta), Marc or [VHTK](https://vhtoolkit.ict.usc.edu/) but can be a good playground to manipulate an interactive character.

# The DialogManager

The big part of this project is the DialogManager. You can follow the existing dialog examples to understand how it works.

A dialog is represented by both a JSON file, listing questions and possible answers, and a C# class inheriting from the Chatbot class, which can be used to add computational logic to the dialog.

During the dialog, the DialogManager is retrieving the possible answers of a question for adding the corresponding buttons to the interface.

No TTS has been integrated yet, the DialogManager expects to find audio files in the Resources folder with file names corresponding to questions' id.

# Credits

Original 3D Model from [Mike Alger](https://mikealger.com/portfolio/avatar#top), prerendered animations from [Mixamo](https://www.mixamo.com)