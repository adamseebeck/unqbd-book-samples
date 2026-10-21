export function focusElement(element) {
    element.focus();
    return document.activeElement === element;
}

export async function notifyTime(dotnet) {
    await dotnet.invokeMethodAsync("ReceiveTime", new Date().toISOString());
}
