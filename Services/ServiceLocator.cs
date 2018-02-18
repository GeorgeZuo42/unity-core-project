﻿using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Core.Services
{
	/// <summary>
	/// Main accessor for global systems like UI, Asset Bundles, Audio or Game Instance.
	/// </summary>
	public class ServiceLocator : MonoBehaviour
	{
		[SerializeField]
		private static GameConfiguration configuration;

		//Contains collection of running services
		private static Dictionary<string, IService> services;
		private static ServiceLocator _instance;

		//Signal is triggered when all services are loaded and running. 
		private static Subject<ServiceLocator> onGameStart = new Subject<ServiceLocator>();
		internal static IObservable<ServiceLocator> OnGameStart { get { return onGameStart; } }

		public static ServiceLocator Instance { get { return _instance; } }

		private static Game gameInstance;
		public static Game GameInstance { get { return gameInstance; } }

		/// <summary>
		/// Creates and initializes all services.
		/// </summary>
		/// <param name="game"></param>
		/// <returns></returns>
		public static IObservable<ServiceLocator> SetUp(Game game)
		{
			return Observable.Create<ServiceLocator>(
				(IObserver<ServiceLocator> observer)=>
				{
					gameInstance = game;
					Instantiate(game);
					var subject = new Subject<ServiceLocator>();

					int servicesCreated = 0;
					if (configuration.disableLogging)
						Debug.unityLogger.logEnabled = false;

					Action<ConfigurationServiceName> OnServiceCreated = configServiceName =>
					{
						servicesCreated++;
						AddService(configServiceName.name, configServiceName.service);

						if (servicesCreated.Equals(configuration.services.Count))
						{
							Debug.Log(("ServiceLocator: " + services.Count + " Services created and active").Colored(Colors.Lime));

							onGameStart.OnNext(_instance);
							onGameStart.OnCompleted();

							observer.OnNext(_instance);
							observer.OnCompleted();
						}

					};

					Debug.Log(("GameConfiguration: Starting Services").Colored(Colors.Lime));
					foreach (var service in configuration.services)
					{
						Debug.Log(("--- Starting Service: " + service.name).Colored(Colors.Cyan));
						service.CreateService().Subscribe(OnServiceCreated);
					}

					return subject.Subscribe();
				});
		}

		/// <summary>
		/// Gets active service.
		/// </summary>
		/// <returns>Service</returns>
		public static T GetService<T>()where T : class, IService
		{
			if (!_instance)return null;

			if (services == null)services = new Dictionary<string, IService>();
			foreach (var serviceKVP in services)
				if (serviceKVP.Value is T)return (T)serviceKVP.Value;

			return null;
		}

		private static void Instantiate(Game game)
		{
			GameObject go = new GameObject(Constants.ServiceLocator);
			if (!_instance)
				_instance = go.AddComponent<ServiceLocator>();

			configuration = game.GameConfiguration;
			services = new Dictionary<string, IService>();
			DontDestroyOnLoad(_instance.gameObject);
		}

		internal static void AddService(string name, IService service)
		{
			if (services == null)services = new Dictionary<string, IService>();
			if (service == null)
			{
				throw new System.Exception("Cannot add a null service to the ServiceLocator");
			}
			services.Add(name, service);
			service.StartService().Subscribe();
		}

		internal static T RemoveService<T>(string serviceName)where T : class, IService
		{
			T returningService = GetService<T>();
			if (returningService != null)
			{
				services[serviceName].StopService();
				services.Remove(serviceName);
			}
			return returningService;
		}

		internal static T RemoveService<T>()where T : class, IService
		{
			if (services == null)services = new Dictionary<string, IService>();
			foreach (var serviceKVP in services)
			{
				if (serviceKVP.Value is T)
				{
					T rtn = (T)serviceKVP.Value;
					services[serviceKVP.Key].StopService();
					services.Remove(serviceKVP.Key);
					return rtn;
				}
			}
			return null;
		}

		private void OnDestroy()
		{
			onGameStart.Dispose();
		}
	}

	public interface IService
	{
		IObservable<IService> StartService();

		IObservable<IService> StopService();

		IObservable<IService> Configure(ServiceConfiguration config);
	}
}