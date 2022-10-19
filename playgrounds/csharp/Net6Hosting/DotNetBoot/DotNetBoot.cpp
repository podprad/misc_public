#include <iostream>
#include <Windows.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <string.h>

typedef void (*void_fn)();

using string_t = std::basic_string<char_t>;

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

    // Try get the pointer,
    error = get_function_pointer_fun(dotnetTypeName, dotnetMethodName, NULL, ctx, NULL, (void**)&dotnetMethod);
    if (error != 0)
    {
        printf("get_function_pointer_fun failed with code %X08\r\n", error);
    }
    else
    {
        dotnetMethod(nullptr, 0);
    }

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

int wmain(int argc, wchar_t **argv)
{
    SetEnvironmentVariable(L"COREHOST_TRACE", L"1");
    SetEnvironmentVariable(L"COREHOST_TRACE_VERBOSITY", L"4");

    int result = 0;

    result = run_hostfxr_example(argc, argv);

    return result;
}