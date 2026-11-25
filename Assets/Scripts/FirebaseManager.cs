using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Firebase 仅用于原生平台（Android/iOS）的 analytics。
// 本项目目标是 WebGL 抖音小游戏，不需要 Firebase；默认不定义 FIREBASE_ENABLED，
// 因此下面的 Firebase 初始化代码不会编译、也不会运行，避免 Editor 下因缺少
// google-services.json / google-services-desktop.json 而每帧报错。
// 若将来要启用 Firebase：先在 Player Settings > Other Settings > Scripting Define Symbols
// 增加 FIREBASE_ENABLED，并把对应平台的配置文件放入 Assets/StreamingAssets/。
#if FIREBASE_ENABLED
using Firebase;
using Firebase.Analytics;
#endif

public class FirebaseManager : MonoBehaviour
{

#if FIREBASE_ENABLED
	DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
#endif

	public static FirebaseManager instance;

	void Awake()
	{
		if (FindObjectsOfType(typeof(FirebaseManager)).Length > 1)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;

		DontDestroyOnLoad(gameObject);
	}

	// Use this for initialization
	void Start()
	{
#if FIREBASE_ENABLED
		Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
			var dependencyStatus = task.Result;
			if (dependencyStatus == Firebase.DependencyStatus.Available)
			{
				// Create and hold a reference to your FirebaseApp,
				// where app is a Firebase.FirebaseApp property of your application class.
				InitializeFirebase();

				// Set a flag here to indicate whether Firebase is ready to use by your app.
			}
			else
			{
				UnityEngine.Debug.LogError(System.String.Format(
				  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
				// Firebase Unity SDK is not safe to use here.
			}
		});
#endif
	}

	// Update is called once per frame
	void Update()
	{

	}

	// Handle initialization of the necessary firebase modules:
#if FIREBASE_ENABLED
	void InitializeFirebase()
	{
		//DebugLog("Enabling data collection.");
		FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

		//DebugLog("Set user properties.");
		// Set the user's sign up method.
		FirebaseAnalytics.SetUserProperty(
			FirebaseAnalytics.UserPropertySignUpMethod,
			"Google");
		// Set the user ID.
		FirebaseAnalytics.SetUserId("uber_user_510");
	}
#endif

	public void LogScreen(string _log)
	{
#if FIREBASE_ENABLED
		FirebaseAnalytics.LogEvent(_log);
#endif
	}

}
