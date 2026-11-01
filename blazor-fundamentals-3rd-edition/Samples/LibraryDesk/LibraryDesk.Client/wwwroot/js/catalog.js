export function focusElement(element) {
    element.focus();
    return document.activeElement === element;
}

export async function notifyTime(dotnet) {
    const now = new Date().toISOString();
    await dotnet.invokeMethodAsync("ReceiveTime", now);
}
