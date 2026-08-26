// be ware that there will be some weird stuff with whitespaces and such as this code was written in notepad (i was lazy to use an Actual IDE)

using BepInEx;
using NLua;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[BepInPlugin("test.lualoader", "Lua Loader Test", "1.0.0")]
public class LuaLoader : BaseUnityPlugin
{
    private Lua lua;

    // helper

    private object ConvertArgument(object value, Type targetType)
    {
    	if (value == null)
    	    return null;

    	if (targetType.IsInstanceOfType(value))
    	    return value;

    	if (targetType.IsEnum)
    	{
    	    if (value is string)
    	        return Enum.Parse(targetType, value.ToString());

    	    return Enum.ToObject(
     	       targetType,
     	       Convert.ChangeType(
     	           value,
     	           Enum.GetUnderlyingType(targetType)
     	        )
        	);
    	}

    	if (targetType == typeof(float))
    	    return Convert.ToSingle(value);

    	if (targetType == typeof(double))
    	    return Convert.ToDouble(value);

    	if (targetType == typeof(int))
    	    return Convert.ToInt32(value);

    	if (targetType == typeof(long))
    	    return Convert.ToInt64(value);

    	if (targetType == typeof(short))
    	    return Convert.ToInt16(value);

    	if (targetType == typeof(byte))
    	    return Convert.ToByte(value);

    	if (targetType == typeof(bool))
    	    return Convert.ToBoolean(value);

    	if (targetType == typeof(string))
    	    return Convert.ToString(value);

    	return Convert.ChangeType(value, targetType);
    }

    // =========================================================
    // lua logging
    // =========================================================

    private void LuaLog(object message)
    {
        Logger.LogInfo("[Lua] " + message);
    }

    // =========================================================
    // enum stuff
    // ========================================================= 

    private Array GetEnumValues(Type type)
    {
	if (type == null || !type.IsEnum)
	    return null;

	return Enum.GetValues(type);
    }

    // =========================================================
    // find a C# type by name
    // =========================================================

    private Type FindType(string name)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);

            if (type != null)
                return type;
        }

        return null;
    }

    // =========================================================
    // create object
    // ========================================================= 

    private object NewObject(Type type, NLua.LuaTable args)
    {
    	if (type == null)
    	{
    	    Logger.LogError("[NewObject] Type is null!");
    	    return null;
    	}

    	try
    	{
    	    ConstructorInfo[] constructors = type.GetConstructors();

    	    foreach (ConstructorInfo constructor in constructors)
    	    {
    	        ParameterInfo[] parameters = constructor.GetParameters();

    	        if (parameters.Length != args.Keys.Count)
    	            continue;

    	        object[] values = new object[parameters.Length];

        	    for (int i = 0; i < parameters.Length; i++)
        	    {
        	        values[i] = ConvertArgument(
     	                args[i + 1],
             	        parameters[i].ParameterType
            	    );
            	}

            	return constructor.Invoke(values);
            }

        	Logger.LogError(
        	    "[NewObject] No matching constructor found for " +
        	    type.FullName
        	);

        	return null;
    	}
    	catch (Exception e)
    	{
    	    Logger.LogError("[NewObject] Failed:");
    	    Logger.LogError(e.ToString());

    	    return null;
    	}
    }

    // =========================================================
    // assembly types stuff which hurts my brain
    // ========================================================= 
    
    private void DumpAssemblyTypes(string assemblyName)
    {
    	foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    	{
    	    if (assembly.GetName().Name == assemblyName)
    	    {
    	        Logger.LogInfo("[Assembly] === " + assemblyName + " ===");

    	        foreach (Type type in assembly.GetTypes())
    	        {
    	            Logger.LogInfo("[Assembly] " + type.FullName);
    	        }

     	       return;
     	   }
    	}

    	Logger.LogError("[Assembly] Not found: " + assemblyName);
    }

    // =========================================================
    // invoke method
    // =========================================================

    private object InvokeMethod(MethodInfo method, object[] args)
    {
    	return method.Invoke(null, args);
    }

    // =========================================================
    // static method calls
    // =========================================================

    private object CallStatic(Type type, string methodName, NLua.LuaTable args)
    {
    	if (type == null)
    	{
    	    Logger.LogError("[CallStatic] Type is null!");
    	    return null;
    	}

    	MethodInfo[] methods = type.GetMethods(
    	    BindingFlags.Public |
    	    BindingFlags.Static
    	);

    	foreach (MethodInfo method in methods)
    	{
    	    if (method.Name != methodName)
    	        continue;

    	    ParameterInfo[] parameters = method.GetParameters();

    	    object[] values = new object[parameters.Length];

    	    for (int i = 0; i < parameters.Length; i++)
    	    {
    	        values[i] = ConvertArgument(
		    args[i + 1],
		    parameters[i].ParameterType
		);
    	    }

    	    try
    	    {
            	return method.Invoke(null, values);
    	    }
    	    catch (Exception e)
    	    {
            	Logger.LogError(
            	    "[CallStatic] Error calling " +
            	    type.FullName +
            	    "." +
            	    methodName
            	);

            	Logger.LogError(e.ToString());

            	return null;
    	    }
    	}

    	Logger.LogError(
    	    "[CallStatic] Method not found: " +
    	    type.FullName +
    	    "." +
    	    methodName
    	);

    	return null;
    }

    // =========================================================
    // find an existing unity object
    // =========================================================

    private UnityEngine.Object FindObject(Type type)
    {
        if (type == null)
        {
            Logger.LogError("[FindObject] Type is null!");
            return null;
        }

        Logger.LogInfo("[FindObject] Searching for: " + type.FullName);

        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(type);

        Logger.LogInfo(
            "[FindObject] Found " +
            objects.Length +
            " objects"
        );

        if (objects.Length == 0)
            return null;

        Logger.LogInfo("[FindObject] Returning: " + objects[0]);

        return objects[0];
    }

    private UnityEngine.Object[] FindObjects(Type type)
    {
    	if (type == null)
    	{
    	    Logger.LogError("[FindObjects] Type is null!");
    	    return Array.Empty<UnityEngine.Object>();
    	}

    	Logger.LogInfo(
    	    "[FindObjects] Searching for: " +
    	    type.FullName
    	);

    	UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(type);

    	Logger.LogInfo(
    	    "[FindObjects] Found " +
    	    objects.Length +
    	    " objects"
    	);

    	return objects;
    }

    // =========================================================
    // run a lua function after a delay
    // =========================================================

    private void RunAfter(
        float seconds,
        string functionName
    )
    {
        StartCoroutine(
            RunAfterCoroutine(
                seconds,
                functionName
            )
        );
    }

    private IEnumerator RunAfterCoroutine(
        float seconds,
        string functionName
    )
    {
        yield return new WaitForSeconds(seconds);

        try
        {
            if (lua == null)
                yield break;

            LuaFunction function = lua.GetFunction(functionName);

            if (function != null)
            {
                function.Call();
            }
            else
            {
                Logger.LogError("[Lua] Function not found: " + functionName);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("[Lua] Callback error:");

            Logger.LogError(e.ToString());
        }
    }

    // =========================================================
    // wait until a unity object exists
    // =========================================================

    private void WaitForObjectLua(
	string typeName,
	string functionName
    )
    {
        Type type = FindType(typeName);

        if (type == null)
        {
            Logger.LogError(
                "[WaitForObject] Couldn't find type: " +
                typeName
            );

            return;
        }

        StartCoroutine(
            WaitForObjectCoroutine(
                type,
                functionName
            )
        );
    }

    private IEnumerator WaitForObjectCoroutine(
        Type type,
        string functionName
    )
    {
        Logger.LogInfo(
            "[WaitForObject] Waiting for: " +
            type.FullName
        );

        while (true)
        {
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(type);

            if (objects.Length > 0)
            {
                UnityEngine.Object obj = objects[0];

                Logger.LogInfo("[WaitForObject] Found: " + obj);

                try
                {
                    if (lua != null)
                    {
                        LuaFunction function = lua.GetFunction(functionName);

                        if (function != null)
                        {
                            function.Call(obj);
                        }
                        else
                        {
                            Logger.LogError(
				"[Lua] Function not found: " +
				functionName
                            );
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("[Lua] WaitForObject callback error:");

                    Logger.LogError(e.ToString());
                }

                yield break;
            }

            yield return null;
        }
    }

    // =========================================================
    // awake thingy
    // =========================================================

    private void Awake()
    {
        Logger.LogInfo("LuaLoader loaded!");

        string luaFolder = Path.Combine(
            Paths.PluginPath,
            "LuaLoader"
        );

        try
        {
            lua = new Lua();

	    // idk what to name this but it loads the CLRPackage for lua
	    // this was specifically made because i didn't want to make a
	    // method specifically for constructors and such
	    lua.LoadCLRPackage();

            // -------------------------------------------------
            // expose loader
            // -------------------------------------------------

            lua["loader"] = this;

            // -------------------------------------------------
            // log()
            // -------------------------------------------------

            MethodInfo logMethod =
                typeof(LuaLoader).GetMethod(
                    "LuaLog",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (logMethod == null)
            {
                Logger.LogError("Could not find LuaLog!");
                return;
            }

            lua.RegisterFunction(
                "log",
                this,
                logMethod
            );

            // -------------------------------------------------
            // get_type()
            // -------------------------------------------------

            MethodInfo findTypeMethod =
                typeof(LuaLoader).GetMethod(
                    "FindType",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (findTypeMethod == null)
            {
                Logger.LogError("Could not find FindType!");
                return;
            }

            lua.RegisterFunction(
                "get_type",
                this,
                findTypeMethod
            );

	    // -------------------------------------------------
            // dump_assembly_types() (i was lazy to put checks and stuff here)
            // -------------------------------------------------

	    lua.RegisterFunction(
    		"dump_assembly_types",
    		this,
    		typeof(LuaLoader).GetMethod(
    		    "DumpAssemblyTypes",
    		    BindingFlags.Instance | BindingFlags.NonPublic
    		)
	    );

	    // -------------------------------------------------
            // invoke_method()
            // -------------------------------------------------

	    lua.RegisterFunction(
    		"invoke_method",
    		this,
    		typeof(LuaLoader).GetMethod(
    		    "InvokeMethod",
    		    BindingFlags.Instance | BindingFlags.NonPublic
    		)
	    );

            // -------------------------------------------------
            // find_object()
            // -------------------------------------------------

            MethodInfo findObjectMethod =
                typeof(LuaLoader).GetMethod(
                    "FindObject",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (findObjectMethod == null)
            {
                Logger.LogError("Could not find FindObject!");
                return;
            }

            lua.RegisterFunction(
                "find_object",
                this,
                findObjectMethod
            );

	    // -------------------------------------------------
            // call_static()
            // -------------------------------------------------

	    lua.RegisterFunction(
    		"call_static",
    		this,
    		typeof(LuaLoader).GetMethod(
    		    "CallStatic",
    		    BindingFlags.Instance |
    		    BindingFlags.NonPublic
    		)
	    );

	    // -------------------------------------------------
            // find_objects()
            // -------------------------------------------------

	    MethodInfo findObjectsMethod =
    	        typeof(LuaLoader).GetMethod(
	        "FindObjects",
	        BindingFlags.Instance |
	        BindingFlags.NonPublic
	    );

	    if (findObjectsMethod == null)
	    {
	    	Logger.LogError("Could not find FindObjects!");
    		return;
	    }

	    lua.RegisterFunction(
    		"find_objects",
    		this,
    		findObjectsMethod
	    );

	    // -------------------------------------------------
            // new_object()
            // -------------------------------------------------

	    lua.RegisterFunction(
    		"new_object",
    		this,
    		typeof(LuaLoader).GetMethod(
    		    "NewObject",
    		    BindingFlags.Instance |
    		    BindingFlags.NonPublic
    		)
	    );

            // -------------------------------------------------
            // run_after()
            // -------------------------------------------------

            MethodInfo runAfterMethod = typeof(LuaLoader).GetMethod(
                    "RunAfter",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (runAfterMethod == null)
            {
                Logger.LogError("Could not find RunAfter!");

                return;
            }

            lua.RegisterFunction(
                "run_after",
                this,
                runAfterMethod
            );

            // -------------------------------------------------
            // wait_for_object()
            // -------------------------------------------------

            MethodInfo waitForObjectMethod = typeof(LuaLoader).GetMethod(
                    "WaitForObjectLua",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (waitForObjectMethod == null)
            {
                Logger.LogError("Could not find WaitForObjectLua!");

                return;
            }

            lua.RegisterFunction(
                "wait_for_object",
                this,
                waitForObjectMethod
            );

	    // -------------------------------------------------
            // getEnumValues()
            // ------------------------------------------------- 

	    MethodInfo getEnumValuesMethod =
	    	typeof(LuaLoader).GetMethod(
        	    "GetEnumValues",
    	   	    BindingFlags.Instance |
        	    BindingFlags.NonPublic
    	        );

	    if (getEnumValuesMethod == null)
	    {
		Logger.LogError("Could not find getEnumValuesMethod!");

		return;  
            }

	    lua.RegisterFunction(
		"get_enum_values",
		this,
		getEnumValuesMethod
	    );

            // -------------------------------------------------
            // expose Assembly-CSharp
            // -------------------------------------------------

            Assembly gameAssembly = Assembly.Load("Assembly-CSharp");
            lua["gameAssembly"] = gameAssembly;

            // -------------------------------------------------
            // expose appDomain
            // -------------------------------------------------

            lua["AppDomain"] = AppDomain.CurrentDomain;

            // -------------------------------------------------
            // lua modules
            // -------------------------------------------------

            lua.DoString(
                "package.path = package.path .. ';"
                + luaFolder.Replace("\\", "/")
                + "/?.lua'"
            );

            // -------------------------------------------------
            // load main.lua
            // -------------------------------------------------

            string mainPath = Path.Combine(
                    luaFolder,
                    "main.lua"
                );

            Logger.LogInfo(
		"Loading: " +
                mainPath
            );

            lua.DoFile(mainPath);

            Logger.LogInfo("Lua executed!");

            // -------------------------------------------------
            // unity scene events
            // -------------------------------------------------

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception e)
        {
            Logger.LogError("Lua error:");

            Logger.LogError(e.ToString());
        }
    }

    // =========================================================
    // unity scene loaded
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        Logger.LogInfo(
            "Scene loaded: " +
            scene.name
        );

        try
        {
            if (lua == null)
                return;

            LuaFunction function = lua.GetFunction("on_scene_loaded");

            if (function != null)
            {
                function.Call(scene.name);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("[Lua] Scene callback error:");

            Logger.LogError(e.ToString());
        }
    }

    // cleanup
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (lua != null)
        {
            lua.Dispose();
            lua = null;
        }
    }
}
