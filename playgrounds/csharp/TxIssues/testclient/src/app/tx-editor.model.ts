import { BehaviorSubject, filter, firstValueFrom } from 'rxjs';

// TX at this moment gives us angular component, however more detailed control can be made only by using raw JavaScript.
// https://docs.textcontrol.com/textcontrol/asp-dotnet/ref.javascript.txtextcontrol.object.htm
// https://docs.textcontrol.com/textcontrol/asp-dotnet/article.client.angular.htm
// https://www.textcontrol.com/blog/2024/09/02/using-the-document-editor-in-spa-applications-using-the-removefromdom-method/

export type LoadDocumentCallback = () => void;

export interface ErrorArgument {
    msg: string;

    handled: boolean;
}

export type ErrorCallback = (errorArgument: ErrorArgument) => void;

export interface LoadSettings {}

export interface TXTextControlType {
    loadDocument(streamType: StreamType, base64data: string, callback?: LoadDocumentCallback | undefined, loadSettings?: LoadSettings | undefined, errorCallback?: ErrorCallback | undefined): void;

    saveDocument(): void;

    addEventListener(eventName: string, callback: () => any): void;

    removeEventListener(eventName: string, callback: () => any): void;

    removeFromDom(): void;
}

export enum StreamType {
    /**
     * Specifies HTML format (Hypertext Markup Language).
     */
    HTMLFormat = 4,

    /**
     * Specifies RTF format (Rich Text Format).
     */
    RichTextFormat = 8,

    /**
     * Specifies text in Unicode format.
     */
    PlainText = 16,

    /**
     * Specifies the internal Text Control format (Unicode).
     */
    InternalUnicodeFormat = 32,

    /**
     * Specifies Microsoft Word format (.DOC version).
     */
    MSWord = 64,

    /**
     * Specifies Adobe Portable Document Format (PDF).
     */
    AdobePDF = 512,

    /**
     * Specifies Microsoft Word format (.DOCX version).
     */
    WordprocessingML = 1024,

    /**
     * Specifies Microsoft Excel format (Office Open XML version).
     */
    SpreadsheetML = 4096,
}

export function getTextControlRef(): Promise<TXTextControlType> {
    const subject = new BehaviorSubject<TXTextControlType | undefined>(undefined);

    let txTextControl = getTextControlRefCore();
    if (txTextControl) {
        subject.next(txTextControl);
    }

    const intervalHandle = setInterval(() => {
        txTextControl = getTextControlRefCore();
        if (txTextControl) {
            subject.next(txTextControl);
            clearInterval(intervalHandle);
        }
    }, 100);

    return firstValueFrom(subject.pipe(filter((ctrl) => !!ctrl)));
}

export function getTextControlRefCore(): TXTextControlType | undefined {
    const anyWindow: any = window;
    if (anyWindow.TXTextControl) {
        const txTextControl = anyWindow.TXTextControl;

        if (txTextControl.loadDocument && txTextControl.init) {
            return txTextControl as TXTextControlType;
        }
    }

    return undefined;
}
