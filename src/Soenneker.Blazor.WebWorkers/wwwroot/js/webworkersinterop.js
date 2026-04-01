function parseJson(value) {
    return typeof value === "string" ? JSON.parse(value) : value;
}

function nowIso() {
    return new Date().toISOString();
}

function resolveBrowserUrl(path) {
    return new URL(path, document.baseURI).toString();
}

function normalizeWorkerType(workerType) {
    if (typeof workerType === "string") {
        return workerType.toLowerCase() === "module" ? "module" : "classic";
    }

    return workerType === 1 ? "module" : "classic";
}

const DEFAULT_POOL_NAME = "default";
const DEFAULT_RUNTIME_SCRIPT_PATH = "_framework/dotnet.js";
const DEFAULT_BOOT_CONFIG_PATH = "_framework/blazor.boot.json";
const DEFAULT_DOTNET_WORKER_SCRIPT_PATH = new URL("./workers/dotnetwebworker.js", import.meta.url).toString();

function normalizePoolName(poolName) {
    return typeof poolName === "string" && poolName.trim().length > 0 ? poolName : DEFAULT_POOL_NAME;
}

export class WebWorkersInterop {
    constructor() {
        this.dotNetReference = null;
        this.pools = new Map();
        this.dotNetPools = new Map();
    }

    initialize(dotNetReference) {
        this.dotNetReference = dotNetReference;
    }

    createPool(optionsJson) {
        const options = parseJson(optionsJson);

        if (options?.backend === "DotNet" || options?.backend === 1) {
            return this.createDotNetPool(options);
        }

        const poolName = normalizePoolName(options?.name);

        if (!options?.scriptPath) {
            throw new Error("Worker script path is required.");
        }

        const workerCount = Math.max(1, options.workerCount ?? 1);

        if (this.pools.has(poolName)) {
            this.destroyPool(poolName);
        }

        const pool = {
            name: poolName,
            scriptPath: options.scriptPath,
            workerType: normalizeWorkerType(options.workerType),
            restartFaultedWorkers: options.restartFaultedWorkers !== false,
            queue: [],
            runningJobs: new Map(),
            workers: [],
            stats: {
                completedJobs: 0,
                cancelledJobs: 0,
                faultedJobs: 0
            }
        };

        this.pools.set(pool.name, pool);

        for (let i = 0; i < workerCount; i++) {
            pool.workers.push(this.createJsWorkerSlot(pool, i));
        }

        this.drainJsPool(pool);
    }

    poolExists(poolName, backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.dotNetPoolExists(poolName);
        }

        return this.pools.has(poolName);
    }

    runJob(requestJson) {
        const request = parseJson(requestJson);
        const pool = this.getRequiredJsPool(request?.poolName);
        const jobId = request?.jobId ?? request?.requestId;

        if (!jobId) {
            throw new Error("Job id is required.");
        }

        if (!request?.workloadName) {
            throw new Error("Workload name is required.");
        }

        const job = {
            jobId,
            poolName: pool.name,
            workloadName: request.workloadName,
            payload: request.payload ?? null,
            timeoutMs: request.timeoutMs ?? null,
            enqueuedAtUtc: nowIso()
        };

        pool.queue.push(job);
        this.drainJsPool(pool);
    }

    runRequest(requestJson) {
        const request = parseJson(requestJson);

        if (request?.backend === "DotNet" || request?.backend === 1) {
            return this.runDotNetJob(request);
        }

        return this.runJob(request);
    }

    cancelJob(poolName, jobId) {
        const pool = this.getRequiredJsPool(poolName);

        const queuedIndex = pool.queue.findIndex(job => job.jobId === jobId);

        if (queuedIndex >= 0) {
            const [job] = pool.queue.splice(queuedIndex, 1);
            this.emitJsEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: job.jobId,
                jobId: job.jobId,
                workloadName: job.workloadName,
                durationMs: 0,
                errorMessage: "Cancelled before dispatch.",
                timestampUtc: nowIso()
            });
            return;
        }

        const running = pool.runningJobs.get(jobId);

        if (running) {
            running.job.cancellationRequested = true;
            running.slot.worker.postMessage({
                type: "cancel",
                jobId
            });
        }
    }

    cancelRequest(poolName, requestId, backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.cancelDotNetJob(poolName, requestId);
        }

        return this.cancelJob(poolName, requestId);
    }

    destroyPool(poolName, backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.destroyDotNetPool(poolName);
        }

        const pool = this.pools.get(poolName);

        if (!pool) {
            return;
        }

        for (const job of pool.queue) {
            this.emitJsEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: job.jobId,
                jobId: job.jobId,
                workloadName: job.workloadName,
                durationMs: 0,
                errorMessage: "Cancelled when the pool was disposed.",
                timestampUtc: nowIso()
            });
        }

        pool.queue = [];

        for (const running of pool.runningJobs.values()) {
            this.emitJsEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: running.job.jobId,
                jobId: running.job.jobId,
                workloadName: running.job.workloadName,
                workerId: running.slot.workerId,
                durationMs: performance.now() - running.startedAt,
                errorMessage: "Cancelled when the pool was disposed.",
                timestampUtc: nowIso()
            });

            if (running.timeoutHandle) {
                clearTimeout(running.timeoutHandle);
            }

            running.slot.worker.terminate();
        }

        pool.runningJobs.clear();

        for (const slot of pool.workers) {
            try {
                slot.worker.terminate();
            } catch {
            }
        }

        this.pools.delete(poolName);
    }

    getPoolSnapshot(poolName, backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.getDotNetPoolSnapshot(poolName);
        }

        const pool = this.pools.get(poolName);
        return pool ? JSON.stringify(this.buildJsPoolSnapshot(pool)) : null;
    }

    getPoolSnapshots(backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.getDotNetPoolSnapshots();
        }

        return JSON.stringify(Array.from(this.pools.values()).map(pool => this.buildJsPoolSnapshot(pool)));
    }

    getCoordinatorSnapshot(backend = "JavaScript") {
        if (backend === "DotNet" || backend === 1) {
            return this.getDotNetCoordinatorSnapshot();
        }

        const pools = Array.from(this.pools.values()).map(pool => this.buildJsPoolSnapshot(pool));

        return JSON.stringify({
            backend: "JavaScript",
            poolCount: pools.length,
            totalWorkers: pools.reduce((total, pool) => total + pool.workerCount, 0),
            busyWorkers: pools.reduce((total, pool) => total + pool.busyWorkerCount, 0),
            queuedCount: pools.reduce((total, pool) => total + pool.queuedCount, 0),
            runningCount: pools.reduce((total, pool) => total + pool.runningCount, 0),
            completedCount: pools.reduce((total, pool) => total + pool.completedCount, 0),
            cancelledCount: pools.reduce((total, pool) => total + pool.cancelledCount, 0),
            faultedCount: pools.reduce((total, pool) => total + pool.faultedCount, 0),
            pools
        });
    }

    async createDotNetPool(optionsJson) {
        const options = parseJson(optionsJson);
        const poolName = normalizePoolName(options?.name);
        const scriptPath = typeof options?.scriptPath === "string" && options.scriptPath.trim().length > 0
            ? options.scriptPath
            : DEFAULT_DOTNET_WORKER_SCRIPT_PATH;
        const workerUrl = resolveBrowserUrl(scriptPath);

        const workerCount = Math.max(1, options.workerCount ?? 1);

        if (this.dotNetPools.has(poolName)) {
            this.destroyDotNetPool(poolName);
        }

        const pool = {
            name: poolName,
            scriptPath,
            workerUrl,
            workerType: normalizeWorkerType(options.workerType),
            runtimeScriptUrl: resolveBrowserUrl(options?.runtimeScriptPath ?? DEFAULT_RUNTIME_SCRIPT_PATH),
            bootConfigUrl: resolveBrowserUrl(options?.bootConfigPath ?? DEFAULT_BOOT_CONFIG_PATH),
            restartFaultedWorkers: options.restartFaultedWorkers !== false,
            queue: [],
            runningInvocations: new Map(),
            workers: [],
            stats: {
                completedInvocations: 0,
                cancelledInvocations: 0,
                faultedInvocations: 0
            }
        };

        this.dotNetPools.set(pool.name, pool);

        for (let i = 0; i < workerCount; i++) {
            pool.workers.push(this.createDotNetWorkerSlot(pool, i));
        }

        try {
            await Promise.all(pool.workers.map(slot => slot.readyPromise));
        } catch (error) {
            this.destroyDotNetPool(pool.name);
            throw error;
        }
    }

    dotNetPoolExists(poolName) {
        return this.dotNetPools.has(poolName);
    }

    runDotNetJob(requestJson) {
        const request = parseJson(requestJson);
        const pool = this.getRequiredDotNetPool(request?.poolName);
        const requestId = request?.requestId ?? request?.invocationId;

        if (!requestId) {
            throw new Error("Request id is required.");
        }

        if (!request?.methodName) {
            throw new Error("Method name is required.");
        }

        const invocation = {
            invocationId: requestId,
            poolName: pool.name,
            methodName: request.methodName,
            arguments: Array.isArray(request.arguments) ? request.arguments : [],
            timeoutMs: request.timeoutMs ?? null,
            enqueuedAtUtc: nowIso()
        };

        pool.queue.push(invocation);
        this.drainDotNetPool(pool);
    }

    cancelDotNetJob(poolName, invocationId) {
        const pool = this.getRequiredDotNetPool(poolName);

        const queuedIndex = pool.queue.findIndex(invocation => invocation.invocationId === invocationId);

        if (queuedIndex >= 0) {
            const [invocation] = pool.queue.splice(queuedIndex, 1);
            pool.stats.cancelledInvocations++;
            this.emitDotNetEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: invocation.invocationId,
                invocationId: invocation.invocationId,
                methodName: invocation.methodName,
                durationMs: 0,
                errorMessage: "Cancelled before dispatch.",
                timestampUtc: nowIso()
            });
            return;
        }

        const running = pool.runningInvocations.get(invocationId);

        if (!running) {
            return;
        }

        pool.runningInvocations.delete(invocationId);

        if (running.timeoutHandle) {
            clearTimeout(running.timeoutHandle);
        }

        const slot = running.slot;
        slot.isBusy = false;
        slot.activeInvocationId = null;
        slot.activeMethodName = null;
        slot.activeInvocationState = null;
        slot.lastCompletedAtUtc = nowIso();
        slot.lastDurationMs = performance.now() - running.startedAt;

        pool.stats.cancelledInvocations++;

        this.emitDotNetEvent({
            eventType: "cancelled",
            poolName: pool.name,
            requestId: running.invocation.invocationId,
            invocationId: running.invocation.invocationId,
            methodName: running.invocation.methodName,
            workerId: slot.workerId,
            durationMs: slot.lastDurationMs ?? 0,
            errorMessage: "Cancelled while running by terminating the worker.",
            timestampUtc: slot.lastCompletedAtUtc
        });

        this.replaceDotNetWorkerSlot(pool, slot, true);
        this.drainDotNetPool(pool);
    }

    destroyDotNetPool(poolName) {
        const pool = this.dotNetPools.get(poolName);

        if (!pool) {
            return;
        }

        for (const invocation of pool.queue) {
            pool.stats.cancelledInvocations++;
            this.emitDotNetEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: invocation.invocationId,
                invocationId: invocation.invocationId,
                methodName: invocation.methodName,
                durationMs: 0,
                errorMessage: "Cancelled when the pool was disposed.",
                timestampUtc: nowIso()
            });
        }

        pool.queue = [];

        for (const running of pool.runningInvocations.values()) {
            pool.stats.cancelledInvocations++;
            this.emitDotNetEvent({
                eventType: "cancelled",
                poolName: pool.name,
                requestId: running.invocation.invocationId,
                invocationId: running.invocation.invocationId,
                methodName: running.invocation.methodName,
                workerId: running.slot.workerId,
                durationMs: performance.now() - running.startedAt,
                errorMessage: "Cancelled when the pool was disposed.",
                timestampUtc: nowIso()
            });

            if (running.timeoutHandle) {
                clearTimeout(running.timeoutHandle);
            }
        }

        pool.runningInvocations.clear();

        for (const slot of pool.workers) {
            try {
                slot.worker.terminate();
            } catch {
            }
        }

        this.dotNetPools.delete(poolName);
    }

    getDotNetPoolSnapshot(poolName) {
        const pool = this.dotNetPools.get(poolName);
        return pool ? JSON.stringify(this.buildDotNetPoolSnapshot(pool)) : null;
    }

    getDotNetPoolSnapshots() {
        return JSON.stringify(Array.from(this.dotNetPools.values()).map(pool => this.buildDotNetPoolSnapshot(pool)));
    }

    getDotNetCoordinatorSnapshot() {
        const pools = Array.from(this.dotNetPools.values()).map(pool => this.buildDotNetPoolSnapshot(pool));

        return JSON.stringify({
            backend: "DotNet",
            poolCount: pools.length,
            totalWorkers: pools.reduce((total, pool) => total + pool.workerCount, 0),
            busyWorkers: pools.reduce((total, pool) => total + pool.busyWorkerCount, 0),
            queuedCount: pools.reduce((total, pool) => total + pool.queuedCount, 0),
            runningCount: pools.reduce((total, pool) => total + pool.runningCount, 0),
            completedCount: pools.reduce((total, pool) => total + pool.completedCount, 0),
            cancelledCount: pools.reduce((total, pool) => total + pool.cancelledCount, 0),
            faultedCount: pools.reduce((total, pool) => total + pool.faultedCount, 0),
            pools
        });
    }

    dispose() {
        for (const poolName of Array.from(this.pools.keys())) {
            this.destroyPool(poolName);
        }

        for (const poolName of Array.from(this.dotNetPools.keys())) {
            this.destroyDotNetPool(poolName);
        }

        this.dotNetReference = null;
    }

    getRequiredJsPool(poolName) {
        const normalizedPoolName = normalizePoolName(poolName);
        const pool = this.pools.get(normalizedPoolName);

        if (!pool) {
            throw new Error(`Worker pool '${normalizedPoolName}' does not exist.`);
        }

        return pool;
    }

    getRequiredDotNetPool(poolName) {
        const normalizedPoolName = normalizePoolName(poolName);
        const pool = this.dotNetPools.get(normalizedPoolName);

        if (!pool) {
            throw new Error(`.NET worker pool '${normalizedPoolName}' does not exist.`);
        }

        return pool;
    }

    createJsWorkerSlot(pool, index) {
        const worker = new Worker(resolveBrowserUrl(pool.scriptPath), {
            name: `${pool.name}-worker-${index + 1}`,
            type: pool.workerType
        });

        const slot = {
            workerId: `${pool.name}-worker-${index + 1}`,
            worker,
            isBusy: false,
            activeJobId: null,
            activeWorkloadName: null,
            activeJobState: null,
            startedAtUtc: null,
            lastCompletedAtUtc: null,
            lastDurationMs: null
        };

        worker.onmessage = event => {
            this.handleJsWorkerMessage(pool, slot, event.data);
        };

        worker.onerror = error => {
            this.handleJsWorkerError(pool, slot, error);
        };

        return slot;
    }

    handleJsWorkerMessage(pool, slot, message) {
        if (!message?.type) {
            return;
        }

        if (message.type === "ready") {
            this.drainJsPool(pool);
            return;
        }

        const running = message.jobId ? pool.runningJobs.get(message.jobId) : null;

        if (!running) {
            return;
        }

        switch (message.type) {
            case "progress":
                this.emitJsEvent({
                    eventType: "progress",
                    poolName: pool.name,
                    requestId: running.job.jobId,
                    jobId: running.job.jobId,
                    workloadName: running.job.workloadName,
                    workerId: slot.workerId,
                    percent: message.percent ?? 0,
                    completedUnits: message.completedUnits ?? 0,
                    totalUnits: message.totalUnits ?? 0,
                    stage: message.stage ?? null,
                    message: message.message ?? null,
                    timestampUtc: nowIso()
                });
                break;
            case "completed":
                this.completeRunningJsJob(pool, slot, running, {
                    eventType: "completed",
                    result: message.result ?? null,
                    errorMessage: null
                });
                break;
            case "cancelled":
                this.completeRunningJsJob(pool, slot, running, {
                    eventType: "cancelled",
                    result: null,
                    errorMessage: message.errorMessage ?? "Cancelled."
                });
                break;
            case "faulted":
                this.completeRunningJsJob(pool, slot, running, {
                    eventType: "faulted",
                    result: null,
                    errorMessage: message.errorMessage ?? "Worker execution failed."
                });
                break;
        }
    }

    handleJsWorkerError(pool, slot, error) {
        const activeJobId = slot.activeJobId;
        const running = activeJobId ? pool.runningJobs.get(activeJobId) : null;

        if (running) {
            this.completeRunningJsJob(pool, slot, running, {
                eventType: "faulted",
                result: null,
                errorMessage: error?.message ?? "Worker crashed unexpectedly."
            });
        }
    }

    completeRunningJsJob(pool, slot, running, completion) {
        pool.runningJobs.delete(running.job.jobId);

        if (running.timeoutHandle) {
            clearTimeout(running.timeoutHandle);
        }

        slot.isBusy = false;
        slot.activeJobId = null;
        slot.activeWorkloadName = null;
        slot.activeJobState = null;
        slot.lastCompletedAtUtc = nowIso();
        slot.lastDurationMs = performance.now() - running.startedAt;

        if (completion.eventType === "completed") {
            pool.stats.completedJobs++;
        } else if (completion.eventType === "cancelled") {
            pool.stats.cancelledJobs++;
        } else if (completion.eventType === "faulted") {
            pool.stats.faultedJobs++;
        }

        this.emitJsEvent({
            eventType: completion.eventType,
            poolName: pool.name,
            requestId: running.job.jobId,
            jobId: running.job.jobId,
            workloadName: running.job.workloadName,
            workerId: slot.workerId,
            durationMs: slot.lastDurationMs ?? 0,
            result: completion.result,
            errorMessage: completion.errorMessage,
            timestampUtc: slot.lastCompletedAtUtc
        });

        if (completion.eventType === "faulted" && pool.restartFaultedWorkers) {
            const slotIndex = pool.workers.indexOf(slot);
            const replacement = this.createJsWorkerSlot(pool, slotIndex);
            slot.worker.terminate();
            pool.workers.splice(slotIndex, 1, replacement);
        }

        this.drainJsPool(pool);
    }

    drainJsPool(pool) {
        for (const slot of pool.workers) {
            if (slot.isBusy || pool.queue.length === 0) {
                continue;
            }

            const job = pool.queue.shift();
            const startedAt = performance.now();

            slot.isBusy = true;
            slot.activeJobId = job.jobId;
            slot.activeWorkloadName = job.workloadName;
            slot.activeJobState = "Running";
            slot.startedAtUtc = nowIso();

            const running = {
                job,
                slot,
                startedAt,
                timeoutHandle: null
            };

            if (job.timeoutMs && job.timeoutMs > 0) {
                running.timeoutHandle = setTimeout(() => {
                    this.cancelJob(pool.name, job.jobId);
                }, job.timeoutMs);
            }

            pool.runningJobs.set(job.jobId, running);

            slot.worker.postMessage({
                type: "run",
                jobId: job.jobId,
                workloadName: job.workloadName,
                payload: job.payload
            });
        }
    }

    buildJsPoolSnapshot(pool) {
        return {
            backend: "JavaScript",
            name: pool.name,
            scriptPath: pool.scriptPath,
            workerType: pool.workerType === "module" ? "Module" : "Classic",
            workerCount: pool.workers.length,
            busyWorkerCount: pool.workers.filter(worker => worker.isBusy).length,
            queuedCount: pool.queue.length,
            runningCount: pool.runningJobs.size,
            completedCount: pool.stats.completedJobs,
            cancelledCount: pool.stats.cancelledJobs,
            faultedCount: pool.stats.faultedJobs,
            restartFaultedWorkers: pool.restartFaultedWorkers,
            workers: pool.workers.map(worker => ({
                backend: "JavaScript",
                workerId: worker.workerId,
                isBusy: worker.isBusy,
                isReady: true,
                activeRequestId: worker.activeJobId,
                activeName: worker.activeWorkloadName,
                activeState: worker.activeJobState,
                startedAtUtc: worker.startedAtUtc,
                lastCompletedAtUtc: worker.lastCompletedAtUtc,
                lastDurationMs: worker.lastDurationMs
            }))
        };
    }

    createDotNetWorkerSlot(pool, index) {
        const worker = new Worker(pool.workerUrl, {
            name: `${pool.name}-dotnet-worker-${index + 1}`,
            type: pool.workerType
        });

        let readyResolve;
        let readyReject;

        const readyPromise = new Promise((resolve, reject) => {
            readyResolve = resolve;
            readyReject = reject;
        });

        const slot = {
            workerId: `${pool.name}-dotnet-worker-${index + 1}`,
            worker,
            isBusy: false,
            isReady: false,
            activeInvocationId: null,
            activeMethodName: null,
            activeInvocationState: null,
            startedAtUtc: null,
            lastCompletedAtUtc: null,
            lastDurationMs: null,
            readyPromise,
            readyResolve,
            readyReject
        };

        worker.onmessage = event => {
            this.handleDotNetWorkerMessage(pool, slot, event.data);
        };

        worker.onerror = error => {
            this.handleDotNetWorkerError(pool, slot, error);
        };

        worker.postMessage({
            type: "initialize",
            runtimeScriptUrl: pool.runtimeScriptUrl,
            bootConfigUrl: pool.bootConfigUrl
        });

        return slot;
    }

    handleDotNetWorkerMessage(pool, slot, message) {
        if (!message?.type) {
            return;
        }

        if (message.type === "ready") {
            slot.isReady = true;
            slot.readyResolve();
            this.drainDotNetPool(pool);
            return;
        }

        if (message.type === "initialization-faulted") {
            slot.readyReject(new Error(message.errorMessage ?? "Worker runtime failed to initialize."));
            return;
        }

        const running = message.invocationId ? pool.runningInvocations.get(message.invocationId) : null;

        if (!running) {
            return;
        }

        switch (message.type) {
            case "completed":
                this.completeRunningDotNetInvocation(pool, slot, running, {
                    eventType: "completed",
                    result: message.result ?? null,
                    errorMessage: null
                });
                break;
            case "faulted":
                this.completeRunningDotNetInvocation(pool, slot, running, {
                    eventType: "faulted",
                    result: null,
                    errorMessage: message.errorMessage ?? "Worker execution failed."
                });
                break;
        }
    }

    handleDotNetWorkerError(pool, slot, error) {
        if (!slot.isReady) {
            slot.readyReject(new Error(error?.message ?? "Worker runtime failed to initialize."));
            return;
        }

        const activeInvocationId = slot.activeInvocationId;
        const running = activeInvocationId ? pool.runningInvocations.get(activeInvocationId) : null;

        if (running) {
            this.completeRunningDotNetInvocation(pool, slot, running, {
                eventType: "faulted",
                result: null,
                errorMessage: error?.message ?? "Worker crashed unexpectedly."
            });
        }
    }

    completeRunningDotNetInvocation(pool, slot, running, completion) {
        pool.runningInvocations.delete(running.invocation.invocationId);

        if (running.timeoutHandle) {
            clearTimeout(running.timeoutHandle);
        }

        slot.isBusy = false;
        slot.activeInvocationId = null;
        slot.activeMethodName = null;
        slot.activeInvocationState = null;
        slot.lastCompletedAtUtc = nowIso();
        slot.lastDurationMs = performance.now() - running.startedAt;

        if (completion.eventType === "completed") {
            pool.stats.completedInvocations++;
        } else if (completion.eventType === "faulted") {
            pool.stats.faultedInvocations++;
        }

        this.emitDotNetEvent({
            eventType: completion.eventType,
            poolName: pool.name,
            requestId: running.invocation.invocationId,
            invocationId: running.invocation.invocationId,
            methodName: running.invocation.methodName,
            workerId: slot.workerId,
            durationMs: slot.lastDurationMs ?? 0,
            result: completion.result,
            errorMessage: completion.errorMessage,
            timestampUtc: slot.lastCompletedAtUtc
        });

        if (completion.eventType === "faulted" && pool.restartFaultedWorkers) {
            this.replaceDotNetWorkerSlot(pool, slot, false);
        }

        this.drainDotNetPool(pool);
    }

    replaceDotNetWorkerSlot(pool, slot, cancelled) {
        const slotIndex = pool.workers.indexOf(slot);

        try {
            slot.worker.terminate();
        } catch {
        }

        const replacement = this.createDotNetWorkerSlot(pool, slotIndex);
        pool.workers.splice(slotIndex, 1, replacement);

        if (!cancelled) {
            replacement.readyPromise.then(() => this.drainDotNetPool(pool)).catch(() => {
            });
        }
    }

    drainDotNetPool(pool) {
        for (const slot of pool.workers) {
            if (!slot.isReady || slot.isBusy || pool.queue.length === 0) {
                continue;
            }

            const invocation = pool.queue.shift();
            const startedAt = performance.now();

            slot.isBusy = true;
            slot.activeInvocationId = invocation.invocationId;
            slot.activeMethodName = invocation.methodName;
            slot.activeInvocationState = "Running";
            slot.startedAtUtc = nowIso();

            const running = {
                invocation,
                slot,
                startedAt,
                timeoutHandle: null
            };

            if (invocation.timeoutMs && invocation.timeoutMs > 0) {
                running.timeoutHandle = setTimeout(() => {
                    this.cancelDotNetJob(pool.name, invocation.invocationId);
                }, invocation.timeoutMs);
            }

            pool.runningInvocations.set(invocation.invocationId, running);

            slot.worker.postMessage({
                type: "invoke",
                invocationId: invocation.invocationId,
                methodName: invocation.methodName,
                arguments: invocation.arguments
            });
        }
    }

    buildDotNetPoolSnapshot(pool) {
        return {
            backend: "DotNet",
            name: pool.name,
            scriptPath: pool.scriptPath,
            workerType: pool.workerType === "module" ? "Module" : "Classic",
            runtimeScriptPath: pool.runtimeScriptUrl,
            bootConfigPath: pool.bootConfigUrl,
            workerCount: pool.workers.length,
            busyWorkerCount: pool.workers.filter(worker => worker.isBusy).length,
            queuedCount: pool.queue.length,
            runningCount: pool.runningInvocations.size,
            completedCount: pool.stats.completedInvocations,
            cancelledCount: pool.stats.cancelledInvocations,
            faultedCount: pool.stats.faultedInvocations,
            restartFaultedWorkers: pool.restartFaultedWorkers,
            workers: pool.workers.map(worker => ({
                backend: "DotNet",
                workerId: worker.workerId,
                isBusy: worker.isBusy,
                isReady: worker.isReady,
                activeRequestId: worker.activeInvocationId,
                activeName: worker.activeMethodName,
                activeState: worker.activeInvocationState,
                startedAtUtc: worker.startedAtUtc,
                lastCompletedAtUtc: worker.lastCompletedAtUtc,
                lastDurationMs: worker.lastDurationMs
            }))
        };
    }

    emitJsEvent(event) {
        if (!this.dotNetReference) {
            return;
        }

        event.backend = "JavaScript";
        this.dotNetReference.invokeMethodAsync("HandleCoordinatorEvent", JSON.stringify(event));
    }

    emitDotNetEvent(event) {
        if (!this.dotNetReference) {
            return;
        }

        event.backend = "DotNet";
        this.dotNetReference.invokeMethodAsync("HandleDotNetCoordinatorEvent", JSON.stringify(event));
    }
}

window.WebWorkersInterop = new WebWorkersInterop();
