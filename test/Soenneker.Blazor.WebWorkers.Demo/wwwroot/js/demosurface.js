window.WebWorkersDemoSurface = {
    runBlockingPrimeAnalysis(upperBound) {
        const limit = Number.parseInt(upperBound, 10) > 0 ? Number.parseInt(upperBound, 10) : 140000;
        const startedAt = performance.now();
        const primes = [];
        let checksum = 0;

        for (let candidate = 2; candidate <= limit; candidate++) {
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
                checksum = (checksum + candidate) % 1000000007;
            }
        }

        return JSON.stringify({
            upperBound: limit,
            primeCount: primes.length,
            largestPrime: primes[primes.length - 1] ?? null,
            checksum,
            durationMs: Math.round(performance.now() - startedAt),
            note: "This run executes on the main UI thread and blocks rendering until it finishes."
        });
    }
};
