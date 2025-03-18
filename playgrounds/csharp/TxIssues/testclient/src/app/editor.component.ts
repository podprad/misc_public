import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { DocumentEditorModule } from '@txtextcontrol/tx-ng-document-editor';
import { BehaviorSubject, combineLatest, filter, Subscription } from 'rxjs';
import { StreamType, TXTextControlType } from './tx-editor.model';

export interface TxEditorInitializeParameters {
    documentType: StreamType;
    documentBase64: string;
}

declare const TXTextControl: TXTextControlType;

@Component({
    selector: 'my-editor',
    standalone: true,
    imports: [AsyncPipe, DocumentEditorModule],
    templateUrl: './editor.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditorComponent implements OnInit, OnDestroy {
    private readonly txTextControlRef$: BehaviorSubject<TXTextControlType | undefined> = new BehaviorSubject<TXTextControlType | undefined>(undefined);
    private readonly initParameters$: BehaviorSubject<TxEditorInitializeParameters | undefined> = new BehaviorSubject<TxEditorInitializeParameters | undefined>(undefined);

    private readonly onReadySubscription: Subscription;

    constructor() {
        this.onReadySubscription = combineLatest([this.txTextControlRef$, this.initParameters$])
            .pipe(filter(([txTextControlRef, initParameters]) => txTextControlRef !== undefined && initParameters !== undefined))
            .subscribe(([txTextControlRef, initParameters]) => {
                this.loadDocument();
            });
    }

    @HostListener('document:txDocumentEditorLoaded', ['$event'])
    public onTxDocumentEditorLoaded() {
        if (!TXTextControl) {
            throw new Error('TXTextControl not loaded');
        }

        console.log('document:txDocumentEditorLoaded');

        const loadedCallback = () => {
            console.log('textControlLoaded');

            TXTextControl.removeEventListener('textControlLoaded', loadedCallback);            
            this.txTextControlRef$.next(TXTextControl);
        };

        TXTextControl.addEventListener('textControlLoaded', loadedCallback);
    }

    public ngOnInit(): void {
        console.log('ngOnInit()');
    }

    public ngOnDestroy(): void {
        console.log('ngOnDestroy()');

        this.onReadySubscription.unsubscribe();

        this.removeOldControl();
    }

    public async initialize(parameters: TxEditorInitializeParameters): Promise<void> {
        this.initParameters$.next(parameters);
    }

    private removeOldControl() {
        const lastTxControl = TXTextControl;
        if (lastTxControl) {
            console.log('Removing TXTextControl from DOM')
            lastTxControl.removeFromDom();
        }
    }

    private loadDocument() {
        const lastInitParameters = this.initParameters$.value;
        const lastTxControl = this.txTextControlRef$.value;
        if (!!lastInitParameters && !!lastTxControl) {
            console.log('Calling loadDocument()');
            lastTxControl.loadDocument(
                this.initParameters$.value.documentType,
                this.initParameters$.value.documentBase64,
                () => {},
                undefined,
                (errorArgument) => this.logError(errorArgument.msg),
            );
        }
    }

    private logError(msg: string) {
        console.log(msg);
    }
}
