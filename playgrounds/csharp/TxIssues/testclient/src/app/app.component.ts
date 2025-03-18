import { Component, ViewChild, ViewContainerRef } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { EditorComponent } from './editor.component';
import { StreamType } from './tx-editor.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'testclient';

  @ViewChild('container', { read: ViewContainerRef, static: true })
  container!: ViewContainerRef;

  public async addEditor(): Promise<void> {
    const componentRef = this.container.createComponent(EditorComponent);

    await componentRef.instance.initialize({
      documentType: StreamType.HTMLFormat,
      documentBase64: btoa('<strong>Hello World</strong>'),
    });
  }

  public removeEditor(): void {
    this.container.clear();
  }
}
