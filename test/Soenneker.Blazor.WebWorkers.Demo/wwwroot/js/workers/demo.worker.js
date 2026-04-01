const workloads = {
    primeAnalysis: "prime-analysis",
    jsonAnalysis: "json-analysis",
    textFrequency: "text-frequency"
};

let activeJobId = null;
let cancellationRequested = false;

class JobCancelledError extends Error {
    constructor(message = "Cancelled.") {
        super(message);
        this.name = "JobCancelledError";
    }
}

function toInt(value, fallback) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function yieldToEventLoop() {
    await sleep(0);
}

function ensureActiveJob(jobId) {
    if (cancellationRequested || activeJobId !== jobId) {
        throw new JobCancelledError();
    }
}

function postProgress(jobId, percent, completedUnits, totalUnits, stage, message) {
    self.postMessage({
        type: "progress",
        jobId,
        percent: Math.min(100, Math.max(0, percent)),
        completedUnits,
        totalUnits,
        stage,
        message
    });
}

function postCompleted(jobId, result) {
    self.postMessage({
        type: "completed",
        jobId,
        result
    });
}

function postCancelled(jobId, errorMessage) {
    self.postMessage({
        type: "cancelled",
        jobId,
        errorMessage
    });
}

function postFaulted(jobId, errorMessage) {
    self.postMessage({
        type: "faulted",
        jobId,
        errorMessage
    });
}

async function runPrimeAnalysis(jobId, payload) {
    const upperBound = Math.max(5000, toInt(payload?.upperBound, 180000));
    const primes = [];
    let checksum = 0;
    let largestPrime = null;
    let lastReportedBucket = -1;
    const reportEvery = Math.max(250, Math.floor(upperBound / 100));

    postProgress(jobId, 0, 0, upperBound, "priming", "Preparing prime analysis.");

    for (let candidate = 2; candidate <= upperBound; candidate++) {
        ensureActiveJob(jobId);

        let isPrime = true;

        for (let index = 0; index < primes.length; index++) {
            const divisor = primes[index];

            if (divisor * divisor > candidate) {
                break;
            }

            if (candidate % divisor === 0) {
                isPrime = false;
                break;
            }
        }

        if (isPrime) {
            primes.push(candidate);
            largestPrime = candidate;
            checksum = (checksum + candidate) % 1000000007;
        }

        if (candidate === upperBound || candidate % reportEvery === 0) {
            const percent = (candidate / upperBound) * 100;
            const bucket = Math.floor(percent);

            if (bucket !== lastReportedBucket || candidate === upperBound) {
                postProgress(jobId, percent, candidate, upperBound, "computing", `Checked ${candidate.toLocaleString()} candidates.`);
                lastReportedBucket = bucket;
            }

            await yieldToEventLoop();
        }
    }

    postProgress(jobId, 100, upperBound, upperBound, "completed", `Found ${primes.length.toLocaleString()} primes.`);

    return {
        upperBound,
        primeCount: primes.length,
        largestPrime,
        checksum
    };
}

async function runJsonAnalysis(jobId, payload) {
    const objectCount = Math.max(500, toInt(payload?.objectCount, 14000));
    const categoryCount = Math.min(20, Math.max(2, toInt(payload?.categoryCount, 10)));
    const categories = Array.from({ length: categoryCount }, (_, index) => `category-${index + 1}`);
    const records = [];
    const totalsByCategory = {};
    const reportEvery = Math.max(100, Math.floor(objectCount / 40));

    postProgress(jobId, 0, 0, objectCount, "generating", "Generating JSON workload.");

    for (let index = 0; index < objectCount; index++) {
        ensureActiveJob(jobId);

        const category = categories[index % categories.length];
        const score = ((index * 17) % 101) + 1;
        const weight = ((index * 29) % 500) / 10;

        records.push({
            id: index + 1,
            category,
            score,
            weight,
            isHot: score >= 75
        });

        totalsByCategory[category] = (totalsByCategory[category] ?? 0) + 1;

        if (index === objectCount - 1 || index % reportEvery === 0) {
            const completed = index + 1;
            postProgress(jobId, (completed / objectCount) * 65, completed, objectCount, "generating",
                `Generated ${completed.toLocaleString()} records.`);
            await yieldToEventLoop();
        }
    }

    postProgress(jobId, 75, objectCount, objectCount, "analyzing", "Summarizing generated dataset.");
    await yieldToEventLoop();
    ensureActiveJob(jobId);

    let hotCount = 0;
    let totalScore = 0;
    let totalWeight = 0;

    for (let index = 0; index < records.length; index++) {
        ensureActiveJob(jobId);

        const record = records[index];
        totalScore += record.score;
        totalWeight += record.weight;

        if (record.isHot) {
            hotCount++;
        }

        if (index === records.length - 1 || index % reportEvery === 0) {
            const completed = index + 1;
            postProgress(jobId, 75 + ((completed / records.length) * 25), completed, records.length, "analyzing",
                `Analyzed ${completed.toLocaleString()} records.`);
            await yieldToEventLoop();
        }
    }

    return {
        objectCount,
        categoryCount,
        averageScore: Number((totalScore / records.length).toFixed(2)),
        averageWeight: Number((totalWeight / records.length).toFixed(2)),
        hotRecordCount: hotCount,
        largestCategory: Object.entries(totalsByCategory).sort((left, right) => right[1] - left[1])[0]?.[0] ?? null,
        totalsByCategory
    };
}

const wordBank = [
    "worker", "browser", "thread", "pool", "queue", "storage", "stream", "async", "render", "signal",
    "payload", "result", "progress", "module", "interop", "buffer", "system", "service", "latency", "runtime"
];

async function runTextFrequency(jobId, payload) {
    const paragraphCount = Math.max(100, toInt(payload?.paragraphCount, 1800));
    const wordsPerParagraph = Math.max(10, toInt(payload?.wordsPerParagraph, 70));
    const reportEvery = Math.max(20, Math.floor(paragraphCount / 50));
    const paragraphs = [];

    postProgress(jobId, 0, 0, paragraphCount, "generating", "Generating synthetic text corpus.");

    for (let paragraphIndex = 0; paragraphIndex < paragraphCount; paragraphIndex++) {
        ensureActiveJob(jobId);

        const words = [];

        for (let wordIndex = 0; wordIndex < wordsPerParagraph; wordIndex++) {
            const offset = (paragraphIndex * 3 + wordIndex * 7) % wordBank.length;
            words.push(wordBank[offset]);
        }

        paragraphs.push(words.join(" "));

        if (paragraphIndex === paragraphCount - 1 || paragraphIndex % reportEvery === 0) {
            const completed = paragraphIndex + 1;
            postProgress(jobId, (completed / paragraphCount) * 55, completed, paragraphCount, "generating",
                `Generated ${completed.toLocaleString()} paragraphs.`);
            await yieldToEventLoop();
        }
    }

    const frequency = new Map();

    postProgress(jobId, 60, 0, paragraphCount, "counting", "Counting words across the corpus.");

    for (let paragraphIndex = 0; paragraphIndex < paragraphs.length; paragraphIndex++) {
        ensureActiveJob(jobId);

        const words = paragraphs[paragraphIndex].split(" ");

        for (const word of words) {
            frequency.set(word, (frequency.get(word) ?? 0) + 1);
        }

        if (paragraphIndex === paragraphs.length - 1 || paragraphIndex % reportEvery === 0) {
            const completed = paragraphIndex + 1;
            postProgress(jobId, 60 + ((completed / paragraphs.length) * 40), completed, paragraphs.length, "counting",
                `Counted ${completed.toLocaleString()} paragraphs.`);
            await yieldToEventLoop();
        }
    }

    const topWords = Array.from(frequency.entries())
        .sort((left, right) => right[1] - left[1])
        .slice(0, 10)
        .map(([word, count]) => ({ word, count }));

    return {
        paragraphCount,
        wordsPerParagraph,
        uniqueWordCount: frequency.size,
        totalWordCount: paragraphCount * wordsPerParagraph,
        topWords
    };
}

async function runWorkload(jobId, workloadName, payload) {
    switch (workloadName) {
        case workloads.primeAnalysis:
            return runPrimeAnalysis(jobId, payload);
        case workloads.jsonAnalysis:
            return runJsonAnalysis(jobId, payload);
        case workloads.textFrequency:
            return runTextFrequency(jobId, payload);
        default:
            throw new Error(`Unknown workload '${workloadName}'.`);
    }
}

self.onmessage = async event => {
    const message = event.data;

    if (!message?.type) {
        return;
    }

    if (message.type === "cancel") {
        if (message.jobId && message.jobId === activeJobId) {
            cancellationRequested = true;
        }

        return;
    }

    if (message.type !== "run" || !message.jobId) {
        return;
    }

    activeJobId = message.jobId;
    cancellationRequested = false;

    try {
        const result = await runWorkload(message.jobId, message.workloadName, message.payload ?? null);
        ensureActiveJob(message.jobId);
        postCompleted(message.jobId, result);
    } catch (error) {
        if (error instanceof JobCancelledError) {
            postCancelled(message.jobId, "Cancelled.");
        } else {
            const messageText = error instanceof Error ? error.message : "Worker execution failed.";
            postFaulted(message.jobId, messageText);
        }
    } finally {
        activeJobId = null;
        cancellationRequested = false;
    }
};

self.postMessage({
    type: "ready"
});
