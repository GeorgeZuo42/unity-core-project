﻿using System;
using System.Collections;
using System.Collections.Generic;
using Core.Assets;
using Core.Service;
using Core.UI;
using UniRx;
using UnityEngine;

namespace Core.LevelLoaderService
{
	public interface ILevelLoaderService : IService
	{
		IObservable<Level> UnloadLevel(Level level);
		IObservable<Level> LoadLevel(string name);

		Level CurrentLevel { get; }
	}

	public class LevelLoaderService : ILevelLoaderService
	{
		protected LevelLoaderServiceConfiguration configuration;
		protected ServiceLocator app;

		protected AssetService assetService;
		protected IUIService uiService;

		protected Level currentLevel;
		public Level CurrentLevel { get { return currentLevel; } }

		protected string currentLevelName;
		protected UIWindow loadingScreen;

		protected Subject<IService> serviceConfigured = new Subject<IService>();
		public IObservable<IService> ServiceConfigured { get { return serviceConfigured; } }

		protected Subject<IService> serviceStarted = new Subject<IService>();
		public IObservable<IService> ServiceStarted { get { return serviceStarted; } }

		protected Subject<IService> serviceStopped = new Subject<IService>();
		public IObservable<IService> ServiceStopped { get { return serviceStopped; } }

		public void Configure(ServiceConfiguration config)
		{
			configuration = config as LevelLoaderServiceConfiguration;

			serviceConfigured.OnNext(this);
			serviceConfigured.OnCompleted();
		}

		public void StartService(ServiceLocator application)
		{
			app = application;
			serviceStarted.OnNext(this);
			serviceStarted.OnCompleted();

			ServiceLocator.OnGameStart.Subscribe(OnGameStart);
		}

		public void StopService(ServiceLocator application)
		{
			serviceStopped.OnNext(this);
			serviceStopped.OnCompleted();
		}

		protected void OnGameStart(ServiceLocator application)
		{
			uiService = ServiceLocator.GetService<IUIService>();
			assetService = ServiceLocator.GetService<IAssetService>()as AssetService;

			//Load first level - TODO move this elsewhere. LevelLoader should not care which level to load next or first.
			// if (configuration.levels != null && configuration.levels.Count > 0)
			// 	LoadLevel(configuration.levels[0]);
			// else
			// 	Debug.LogError("LevelLoaderService: No levels configured");
		}

		public IObservable<Level> LoadLevel(string name)
		{
			if (currentLevel)
				UnloadLevel(currentLevel);

			BundleNeeded level = new BundleNeeded(AssetCategoryRoot.Levels, name.ToLower(), name.ToLower());
			var observable = new Subject<Level>();

			Action<UnityEngine.Object> OnLevelLoaded = loadedLevel =>
			{
				Resources.UnloadUnusedAssets();
				Debug.Log(("LevelLoaderService: Loaded level - " + loadedLevel.name).Colored(Colors.lightblue));

				currentLevel = GameObject.Instantiate<Level>(loadedLevel as Level);
				currentLevel.name = loadedLevel.name;

				if (loadingScreen)
					loadingScreen.Close();

				observable.OnNext(currentLevel);
				observable.OnCompleted();
			};

			assetService.GetAndLoadAsset<Level>(level)
				.Subscribe(OnLevelLoaded);

			return observable;
		}

		public IObservable<Level> UnloadLevel(Level level)
		{
			var subject = new Subject<Level>();

			if (level)
			{
				Debug.Log(("LevelLoaderService: Unloading level  - " + currentLevel.name).Colored(Colors.lightblue));
				GameObject.Destroy(level.gameObject);
				assetService.UnloadAsset(level.LevelName, true);
			}

			subject.OnNext(null);
			subject.OnCompleted();

			Resources.UnloadUnusedAssets();

			return subject;
		}
	}
}