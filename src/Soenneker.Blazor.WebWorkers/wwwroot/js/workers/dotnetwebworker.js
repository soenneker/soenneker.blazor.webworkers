let assemblyExports = null;
let startupError = null;
let initialized = false;

function getErrorMessage(error) {
    if (error instanceof Error) {
        return error.message;
    }

    return typeof error === "string" ? error : "Worker execution failed.";
}

function resolveExportedMethod(methodName) {
    if (!assemblyExports) {
        throw new Error(startupError || ".NET worker exports not loaded.");
    }

    const segments = methodName.split(".").filter(Boolean);

    if (segments.length === 0) {
        throw new Error("Method name is required.");
    }

    let current = assemblyExports;

    for (const segment of segments) {
        current = current?.[segment];
    }

    if (typeof current !== "function") {
        throw new Error(`Unable to resolve exported method '${methodName}'.`);
    }

    return current;
}

async function initializeRuntime(message) {
    if (initialized) {
        return;
    }

    try {
        const runtimeModule = await import(message.runtimeScriptUrl);
        const { dotnet } = runtimeModule;
        const runtime = await dotnet.create({
            configSrc: message.bootConfigUrl
        });
        const config = runtime.getConfig();

        assemblyExports = await runtime.getAssemblyExports(config.mainAssemblyName);
        startupError = null;
        initialized = true;

        self.postMessage({
            type: "ready"
        });
    } catch (error) {
        startupError = getErrorMessage(error);

        self.postMessage({
            type: "initialization-faulted",
            errorMessage: startupError
        });
    }
}

async function invokeMethod(message) {
    try {
        if (!initialized) {
            throw new Error(startupError || ".NET worker runtime is not initialized.");
        }

        const method = resolveExportedMethod(message.methodName);
        const args = Array.isArray(message.arguments) ? message.arguments : [];
        const result = await method(...args);

        self.postMessage({
            type: "completed",
            invocationId: message.invocationId,
            result: result ?? null
        });
    } catch (error) {
        self.postMessage({
            type: "faulted",
            invocationId: message.invocationId,
            errorMessage: getErrorMessage(error)
        });
    }
}

self.addEventListener("message", async event => {
    const message = event.data;

    if (!message?.type) {
        return;
    }

    switch (message.type) {
        case "initialize":
            await initializeRuntime(message);
            break;
        case "invoke":
            await invokeMethod(message);
            break;
    }
});
