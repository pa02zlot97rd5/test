import { Injectable, InjectionToken, Type } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { DialogCallback, DialogConfig, DialogState, DialogTemplate } from './dialog-config';

@Injectable({
  providedIn: 'root'
})
export class DialogService {

  state: DialogState = {
    CurrentItem: new BehaviorSubject<DialogConfig | undefined>(undefined),
    Queue: []
  };


  get currentItem(): Observable<DialogConfig | undefined> {
    return this.state.CurrentItem;
  }

  constructor() { }

  alert(message: string): DialogConfig | undefined {
    return this.enqueueDialog({ message: message });
  }

  confirm(message: string, callback: DialogCallback): DialogConfig | undefined {
    return this.enqueueDialog({
      message: message,
      callback: callback,
      buttons: ['Cancel', 'OK']
    });
  }

  accept(message: string, callback: DialogCallback): DialogConfig | undefined {
    return this.enqueueDialog({
      message: message,
      callback: callback,
      buttons: ['OK']
    });
  }

  dialog(title: string, message: string, buttons?: string[], callback?: DialogCallback, onshow?: () => void): DialogConfig | undefined {
    return this.enqueueDialog({
      message: message,
      title: title,
      callback: callback,
      buttons: buttons,
      onshow: onshow
    });
  }

  htmlDialog(title: string, htmltemplate: Type<DialogTemplate>, buttons: string[], callback: DialogCallback, onshow?: () => void): DialogConfig | undefined {
    return this.enqueueDialog({
      htmltemplate: htmltemplate,
      title: title,
      callback: callback,
      buttons: buttons,
      onshow: onshow
    });
  }

  textareaDialog(title: string, message: string, placeholder: string | undefined, textarea: string,
    buttons: string[], buttonTemplate: Type<DialogTemplate> | undefined, callback?: DialogCallback, onshow?: () => void): DialogConfig | undefined {
    return this.enqueueDialog({
      enableTextarea: true,
      title: title,
      message: message,
      placeholder: placeholder,
      textarea: textarea,
      callback: callback,
      buttons: buttons,
      buttonTemplate: buttonTemplate,
      onshow: onshow
    });
  }

  enqueueDialog(config: DialogConfig): DialogConfig | undefined {
    if (config.message == null && config.htmltemplate == null && config.enableTextarea == null) {
      return undefined;
    }
    config.title = config.title || 'Information';
    if (config.buttons == null) {
      config.buttons = ['OK'];
    }
    this.state.Queue.push(config);
    if (this.state.CurrentItem.value == null) {
      this.dismissCurrent();
    }

    config.dismiss = () => {
      if (this.state.CurrentItem.value === config) {
        this.dismissCurrent();
      }
    };
    return config;
  }

  dismissCurrent(): void {
    if (this.state.CurrentItem.value != null) {
      if (this.state.CurrentItem.value.ondismiss) {
        this.state.CurrentItem.value.ondismiss();
      }

      this.state.CurrentItem.next(undefined);
    }

    if (this.state.Queue.length > 0) {
      this.state.CurrentItem.next(this.state.Queue[0]);
      this.state.Queue.shift();

      if (this.state.CurrentItem.value!.onshow) {
        this.state.CurrentItem.value!.onshow();
      }
    }
  }

  dismissAll(): void {
    while (this.state.CurrentItem.value != null) {
      this.dismissCurrent();
    }
  }

  notifyInputError(msg: string) {
    return this.dialog('Error', msg);
  }

  connectionError(txt: string): ((msg: string | any) => void);
  connectionError(txt: string | any, msg: any): void;
  connectionError(txt: string | any, msg?: string | any): ((msg: string | any) => void) | void {
    if (typeof txt === 'string') {
      if (msg == null)
        return (msg) => {
          if (msg && msg.error && msg.error.Message)
            this.dialog('Error', txt + msg.error.Message);
          else
            this.dialog('Error', txt + msg.statusText);
        };
    } else {
      msg = txt;
      txt = '';
    }

    if (msg && msg.error && msg.error.Message)
      this.dialog('Error', txt + msg.error.Message);
    else
      this.dialog('Error', txt + msg.statusText);
  }
}