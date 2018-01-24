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
		IObservable<Level> LoadLevel(Levels level);
		IObservable<Level> LoadLevel(string name);

		Level CurrentLevel { get; }

		IObservable<Level> OnLevelLoaded { get; }
		IObservable<Level> OnLevelUnloaded { get; }
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
		protected CompositeDisposable disposables = new CompositeDisposable();

		protected Subject<IService> serviceConfigured = new Subject<IService>();
		public IObservable<IService> ServiceConfigured { get { return serviceConfigured; } }

		protected Subject<IService> serviceStarted = new Subject<IService>();
		public IObservable<IService> ServiceStarted { get { return serviceStarted; } }

		protected Subject<IService> serviceStopped = new Subject<IService>();
		public IObservable<IService> ServiceStopped { get { return serviceStopped; } }

		protected Subject<Level> onLevelLoaded = new Subject<Level>();
		public IObservable<Level> OnLevelLoaded { get { return onLevelLoaded; } }

		protected Subject<Level> onLevelUnloaded = new Subject<Level>();
		public IObservable<Level> OnLevelUnloaded { get { return onLevelUnloaded; } }

		public void Configure(ServiceConfiguration config)
		{
			configuration = config as LevelLoaderServiceConfiguration;

			serviceConfigured.OnNext(this);
		}

		public void StartService(ServiceLocator application)
		{
			app = application;
			serviceStarted.OnNext(this);

			ServiceLocator.OnGameStart.Subscribe(OnGameStart);
		}

		public void StopService(ServiceLocator application)
		{
			serviceStopped.OnNext(this);

			serviceConfigured.Dispose();
			serviceStarted.Dispose();
			serviceStopped.Dispose();

			onLevelLoaded.Dispose();
			onLevelUnloaded.Dispose();

			disposables.Dispose();
		}

		protected void OnGameStart(ServiceLocator application)
		{
			uiService = ServiceLocator.GetService<IUIService>();
			assetService = ServiceLocator.GetService<IAssetService>() as AssetService;

			//Load first level - TODO move this elsewhere. LevelLoader should not care which level to load next or first.
			if (configuration.levels != null && configuration.levels.Count > 0)
				LoadLevel(configuration.levels[0]);
			else
				Debug.LogError("LevelLoaderService: No levels configured");
		}

		public IObservable<Level> LoadLevel(Levels level)
		{
			return LoadLevel(level.ToString());
		}

		public IObservable<Level> LoadLevel(string name)
		{
			if (currentLevel)
				UnloadLevel(currentLevel);

			BundleNeeded level = new BundleNeeded(AssetCategoryRoot.Levels, name.ToLower(), name.ToLower());
			var ret = assetService.GetAndLoadAsset<Level>(level)
				.Subscribe(loadedLevel =>
				{
					Resources.UnloadUnusedAssets();
					Debug.Log(("LevelLoaderService: Loaded level - " + loadedLevel.name).Colored(Colors.lightblue));

					currentLevel = GameObject.Instantiate<Level>(loadedLevel as Level);
					currentLevel.name = loadedLevel.name;

					if (loadingScreen)
						loadingScreen.Close();

					onLevelLoaded.OnNext(currentLevel);
				});

			return ret as IObservable<Level>;
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
			onLevelUnloaded.OnNext(null);

			return subject;
		}
	}
}