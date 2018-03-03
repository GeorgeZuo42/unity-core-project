﻿using Core.Services;
using Core.Services.Audio;
using Core.Services.UI;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace Core.Services.Levels
{
	public enum LevelState
	{
		Loaded,
		Started,
		InProgress,
		Completed
	}

	/// <summary>
	/// A level has the same purpose of a scene, but we can change them without having to load a
	/// scene. This works well on most plaforms except for WebGL where loading scenes also clears the memory.
	/// </summary>
	public abstract class Level : CoreBehaviour
	{
		public AudioPlayer backgroundMusic;

		[Inject]
		protected ILevelLoaderService levelService;

		[Inject]
		protected IAudioService audioService;

		[Inject]
		protected IUIService uiService;

		protected LevelState levelState;
		public LevelState State { get { return levelState; } }

		protected override void Awake()
		{
			base.Awake();
			Debug.Log(("Level: " + name + " loaded").Colored(Colors.LightBlue));

			//levelService = ServiceLocator.GetService<ILevelLoaderService>();
			//audioService = ServiceLocator.GetService<IAudioService>();
			//uiService = ServiceLocator.GetService<IUIService>();

			levelState = LevelState.Loaded;
		}

		protected override void Start()
		{
			base.Start();

			Debug.Log(("Level: " + name + " started").Colored(Colors.LightBlue));

			levelState = LevelState.Started;
			levelState = LevelState.InProgress;

			if (audioService != null && backgroundMusic != null && backgroundMusic.Clip != null)
				audioService.PlayMusic(backgroundMusic);
		}

		public virtual void Unload()
		{
			if (audioService != null && backgroundMusic != null && backgroundMusic.Clip != null)
				audioService.StopClip(backgroundMusic);
		}

		protected virtual void OnDestroy()
		{
			if (audioService != null && backgroundMusic != null && backgroundMusic.Clip != null)
				audioService.StopClip(backgroundMusic);
		}
	}
}