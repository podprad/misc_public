#include <iostream>
#include <Windows.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <string.h>
#include <coreclrhost.h>

typedef void (*void_fn)();

using string_t = std::basic_string<char_t>;
using string = std::basic_string<char>;

int run_hostfxr_example(int argc, wchar_t** argv);

typedef const int (*dotnet_main_void_ptr)();

void BuildTpaList(const char* directory, const char* extension, std::string& tpaList);

int run_coreclr_direct_example(int argc, wchar_t** argv);

#define CORE_CLR_DIR L"c:\\Program Files (x86)\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.8\\"

int wmain(int argc, wchar_t **argv)
{
    SetEnvironmentVariable(L"COREHOST_TRACE", L"1");
    SetEnvironmentVariable(L"COREHOST_TRACE_VERBOSITY", L"4");

    int result = run_hostfxr_example(argc, argv);
    // int result = run_coreclr_direct_example(argc, argv);
   
    return result;
}

int run_coreclr_direct_example(int argc, wchar_t** argv)
{
    wchar_t localDirectory_t[MAX_PATH];
    GetFullPathName(argv[0], MAX_PATH, localDirectory_t, NULL);

    auto last_slash = wcsrchr(localDirectory_t, '\\');
    if (last_slash != NULL)
        *last_slash = 0;

    // Construct the managed library path
    string_t managedLibraryPath_t(localDirectory_t);
    managedLibraryPath_t.append(L"\\MyLibrary.dll");
    string managedLibraryPath(managedLibraryPath_t.begin(), managedLibraryPath_t.end());

    // Construct coreclr.dll local path
    string_t coreclrPath_t(localDirectory_t);
    coreclrPath_t.append(L"\\coreclr.dll");

    // Construct local path
    string_t localDirectory_st = localDirectory_t;
    string localDirectory_s(localDirectory_st.begin(), localDirectory_st.end());

    int result = 0;

    std::string tpaList;

    auto coreClrHandle = LoadLibrary(coreclrPath_t.c_str());
    
    // Include DLLs from local dir
    BuildTpaList(localDirectory_s.c_str(), ".dll", tpaList);

    if (coreClrHandle == NULL)
    {
        coreclrPath_t.clear();
        coreclrPath_t.append(CORE_CLR_DIR);
        coreclrPath_t.append(L"coreclr.dll");

        coreClrHandle = LoadLibrary(coreclrPath_t.c_str());

        string_t coreclrDir_t = CORE_CLR_DIR;
        string coreclrDir(coreclrDir_t.begin(), coreclrDir_t.end());

        // Include dlls from coreclr dir
        BuildTpaList(coreclrDir.c_str(), ".dll", tpaList);
    }

    if (coreClrHandle == NULL)
    {
        std::cout << "Failed to load coreclr.dll";
        return 0;
    }

    string coreClrPath(coreclrPath_t.begin(), coreclrPath_t.end());    

    coreclr_initialize_ptr coreclr_initialize = (coreclr_initialize_ptr)GetProcAddress(coreClrHandle, "coreclr_initialize");
    coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClrHandle, "coreclr_create_delegate");

    void* hostHandle = 0;
    unsigned int domainId = 0;  

    // Define CoreCLR properties
    const char* propertyKeys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",      // Trusted assemblies (like the GAC)
        "APP_PATHS",                        // Directories to probe for application assemblies
        // "APP_NI_PATHS",                     // Directories to probe for application native images (not used in this sample)
        // "NATIVE_DLL_SEARCH_DIRECTORIES",    // Directories to probe for native dlls (not used in this sample)
    };

    const char* propertyValues[] = {
        tpaList.c_str(),
        localDirectory_s.c_str(),
    };

    result = coreclr_initialize(localDirectory_s.c_str(), "MyAppDomain", sizeof(propertyKeys) / sizeof(char*), propertyKeys, propertyValues, &hostHandle, &domainId);

    dotnet_main_void_ptr managedDelegate;
    result = createManagedDelegate(
        hostHandle,
        domainId,
        "MyLibrary",
        "MyLibrary.ClassWithMain",
        "DotnetMainVoid",
        (void**)&managedDelegate);

    if (result != 0)
    {
        std::cout << "Error " << result << "\r\n";
        return result;
    }

    managedDelegate();
}

void BuildTpaList(const char* directory, const char* extension, std::string& tpaList)
{
    std::string searchPath(directory);
    searchPath.append("\\");
    searchPath.append("*");
    searchPath.append(extension);

    WIN32_FIND_DATAA findData;
    HANDLE fileHandle = FindFirstFileA(searchPath.c_str(), &findData);

    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        do
        {
            // Append the assembly to the list
            tpaList.append(directory);
            tpaList.append("\\");
            tpaList.append(findData.cFileName);
            tpaList.append(";");

            // Note that the CLR does not guarantee which assembly will be loaded if an assembly
            // is in the TPA list multiple times (perhaps from different paths or perhaps with different NI/NI.dll
            // extensions. Therefore, a real host should probably add items to the list in priority order and only
            // add a file if it's not already present on the list.
            //
            // For this simple sample, though, and because we're only loading TPA assemblies from a single path,
            // and have no native images, we can ignore that complication.
        } while (FindNextFileA(fileHandle, &findData));
        FindClose(fileHandle);
    }
}

int run_hostfxr_example(int argc, wchar_t** argv)
{
    int error = 0;

    // Locate hostfxt
    char_t hostfxrPath[MAX_PATH];
    size_t buffer_size = sizeof(hostfxrPath) / sizeof(char_t);
    error = get_hostfxr_path(hostfxrPath, &buffer_size, nullptr);
    if (error != 0)
    {
        printf_s("Could not locate hostfxr.dll. Error: 0x%08X\r\n", error);
        return -1;
    }

    // Load hostfxr and delegates.
    printf_s("Loading %ws\r\n", hostfxrPath);
    auto hostfxrHandle = LoadLibrary(hostfxrPath);
    if (hostfxrHandle == 0)
    {
        printf_s("Could not load %ws\r\n", hostfxrPath);
        return -1;
    }

    auto hostfxr_initialize_for_runtime_config = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(hostfxrHandle, "hostfxr_initialize_for_runtime_config");
    if (hostfxr_initialize_for_runtime_config == NULL)
    {
        printf_s("Could not find hostfxr_initialize_for_runtime_config\r\n");
        return -1;
    }

    auto hostfxr_get_runtime_delegate = (hostfxr_get_runtime_delegate_fn)GetProcAddress(hostfxrHandle, "hostfxr_get_runtime_delegate");
    if (hostfxr_get_runtime_delegate == NULL)
    {
        printf_s("Could not find hostfxr_get_runtime_delegate\r\n");
        return -1;
    }

    auto hostfxr_set_runtime_property_value = (hostfxr_set_runtime_property_value_fn)GetProcAddress(hostfxrHandle, "hostfxr_set_runtime_property_value");
    if (hostfxr_set_runtime_property_value == NULL)
    {
        return -1;
    }

    // Initialize and start the .NET Core runtime
    char_t hostfxrDirectory[MAX_PATH];
    GetFullPathName(argv[0], sizeof(hostfxrDirectory) / sizeof(char_t), hostfxrDirectory, nullptr);

    string_t hostfxrDirectoryString = hostfxrDirectory;
    auto dirSeparatorPosition = hostfxrDirectoryString.find_last_of('\\');
    hostfxrDirectoryString = hostfxrDirectoryString.substr(0, dirSeparatorPosition + 1);

    const string_t configPath = hostfxrDirectoryString + L"runtimeconfig.json";

    // Load .NET Core
    hostfxr_handle ctx;
    error = hostfxr_initialize_for_runtime_config(configPath.c_str(), nullptr, &ctx);
    if (error != 0)
    {
        printf("hostfxr_initialize_for_runtime_config failed with code %X08\r\n", error);
        return -1;
    }

    // Set properties
    error = hostfxr_set_runtime_property_value(ctx, L"APP_CONTEXT_BASE_DIRECTORY", hostfxrDirectoryString.c_str());

    // Get coreclr delegates
    // hdt_load_assembly_and_get_function_pointer
    void* load_assembly_and_get_function_pointer_fun_void = nullptr;
    load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer_fun = nullptr;

    error = hostfxr_get_runtime_delegate(ctx, hdt_load_assembly_and_get_function_pointer, &load_assembly_and_get_function_pointer_fun_void);
    if (error != 0)
    {
        printf("hostfxr_get_runtime_delegate failed with code %X08\r\n", error);
        return -1;
    }

    load_assembly_and_get_function_pointer_fun = (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer_fun_void;

    // hdt_get_function_pointer
    void* get_function_pointer_fun_void = nullptr;
    get_function_pointer_fn get_function_pointer_fun = nullptr;

    error = hostfxr_get_runtime_delegate(ctx, hdt_get_function_pointer, &get_function_pointer_fun_void);
    if (error != 0)
    {
        return -1;
    }

    get_function_pointer_fun = (get_function_pointer_fn)get_function_pointer_fun_void;

    // Load managed assembly and get function pointer to a managed method
    const string_t dotnetDllPath = hostfxrDirectoryString + L"MyLibrary.dll";
    const char_t* dotnetTypeName = L"MyLibrary.ClassWithMain, MyLibrary";
    const char_t* dotnetMethodName = L"DotnetMain";

    component_entry_point_fn dotnetMethod = nullptr;

    std::cout << "Press any key\r\n";
    std::cin.get();

    // Try get the pointer,
    // error = get_function_pointer_fun(L"System.Runtime.Assembly, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", L"UnsafeLoadFrom", NULL, NULL, NULL, (void**)&dotnetMethod);
    /*error = get_function_pointer_fun(L"System.Runtime.Assembly", L"UnsafeLoadFrom", NULL, NULL, NULL, (void**)&dotnetMethod);
    if (error != 0)
    {
        printf("get_function_pointer_fun failed with code %X08\r\n", error);
    }
    else
    {
        dotnetMethod(nullptr, 0);
    }*/

    error = load_assembly_and_get_function_pointer_fun(
        dotnetDllPath.c_str(),
        dotnetTypeName,
        dotnetMethodName,
        nullptr /*delegate_type_name*/,
        nullptr,
        (void**)&dotnetMethod);

    if (error != 0)
    {
        printf("load_assembly_and_get_function_pointer_fun failed with code %X08\r\n", error);
        return -1;
    }

    dotnetMethod(nullptr, 0);

    auto myLibraryHandle = LoadLibrary(L"MyLibrary.dll");
    if (myLibraryHandle == 0)
    {
        printf("Could not load MyLibrary.dll\r\n");
        return -1;
    }

    auto anotherHello = (void_fn)GetProcAddress(myLibraryHandle, "HelloDllExport");

    if (anotherHello == 0)
    {
        printf("Exported function AnotherHello not found in MyLibrary.dll\r\n");
        return -1;
    }

    anotherHello();
    anotherHello();

    return 0;
}