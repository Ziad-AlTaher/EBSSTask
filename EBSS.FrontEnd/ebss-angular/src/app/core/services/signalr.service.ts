import { Injectable, inject, signal } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { ApiConfigService } from './api-config.service';

export interface FileReadyNotification {
  processId: string;
  fileName: string;
  downloadUrl: string;
  completedAt: string;
}

export interface FileFailedNotification {
  processId: string;
  errorMessage: string;
  failedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private config = inject(ApiConfigService);
  private hubConnection: signalR.HubConnection | null = null;

  readonly isConnected = signal<boolean>(false);
  readonly fileReady$ = new Subject<FileReadyNotification>();
  readonly fileFailed$ = new Subject<FileFailedNotification>();

  get connectionId(): string | null {
    return this.hubConnection?.connectionId || null;
  }

  startConnection(): Promise<void> {
    if (this.hubConnection) {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected ||
          this.hubConnection.state === signalR.HubConnectionState.Connecting) {
        return Promise.resolve();
      }
    }

    const hubUrl = this.config.getEndpoint('/hubs/reports');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.on('OnFileReady', (raw: any) => {
      // console.log('SignalR: OnFileReady received:', raw);
      const notification: FileReadyNotification = {
        processId: String(raw.processId || raw.ProcessId || ''),
        fileName: String(raw.fileName || raw.FileName || ''),
        downloadUrl: String(raw.downloadUrl || raw.DownloadUrl || ''),
        completedAt: String(raw.completedAt || raw.CompletedAt || new Date().toISOString())
      };
      this.fileReady$.next(notification);
    });

    this.hubConnection.on('OnFileFailed', (raw: any) => {
      // console.warn('SignalR: OnFileFailed received:', raw);
      const notification: FileFailedNotification = {
        processId: String(raw.processId || raw.ProcessId || ''),
        errorMessage: String(raw.errorMessage || raw.ErrorMessage || 'Report generation failed'),
        failedAt: String(raw.failedAt || raw.FailedAt || new Date().toISOString())
      };
      this.fileFailed$.next(notification);
    });

    this.hubConnection.onreconnecting(() => {
      this.isConnected.set(false);
    });

    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
    });

    this.hubConnection.onclose(() => {
      this.isConnected.set(false);
    });

    return this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        // console.log('SignalR connected to ReportHub. ConnectionId:', this.connectionId);
      })
      .catch((err) => {
        this.isConnected.set(false);
        // console.error('SignalR connection failed:', err);
      });
  }

  subscribeToProcess(processId: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('SubscribeToProcess', processId).catch(err => {
        // console.warn('Failed to subscribe to process group:', err);
      });
    }
    return Promise.resolve();
  }
}
