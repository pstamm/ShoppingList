export async function startCamera(videoElement) {
    if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error("Camera access is unavailable. Use a supported browser over HTTPS or localhost.");
    }

    let stream;
    try {
        stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: { ideal: "environment" } },
            audio: false
        });
        videoElement.srcObject = stream;
        await videoElement.play();
    } catch (error) {
        stream?.getTracks().forEach(track => track.stop());
        videoElement.srcObject = null;
        if (error?.name === "NotAllowedError" || error?.name === "SecurityError") {
            throw new Error("Camera permission was denied. Allow camera access in your browser and try again.");
        }

        if (error?.name === "NotFoundError") {
            throw new Error("No camera was found on this device.");
        }

        throw new Error("The camera could not be started. Check that it is not already in use.");
    }
}

export async function captureCamera(videoElement, maxBytes) {
    if (!videoElement.videoWidth || !videoElement.videoHeight) {
        throw new Error("The camera is not ready yet. Wait for the preview and try again.");
    }

    const scale = Math.min(1, 1600 / Math.max(videoElement.videoWidth, videoElement.videoHeight));
    const canvas = document.createElement("canvas");
    canvas.width = Math.round(videoElement.videoWidth * scale);
    canvas.height = Math.round(videoElement.videoHeight * scale);
    canvas.getContext("2d").drawImage(videoElement, 0, 0, canvas.width, canvas.height);

    const blob = await new Promise(resolve => canvas.toBlob(resolve, "image/jpeg", 0.85));
    if (!blob) {
        throw new Error("The camera image could not be captured.");
    }

    if (blob.size > maxBytes) {
        throw new Error("The captured photo exceeds the 5 MB upload limit. Move closer and try again.");
    }

    return new Uint8Array(await blob.arrayBuffer());
}

export function stopCamera(videoElement) {
    const stream = videoElement.srcObject;
    if (stream instanceof MediaStream) {
        stream.getTracks().forEach(track => track.stop());
    }

    videoElement.srcObject = null;
}
