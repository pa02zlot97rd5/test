import { Component, EventEmitter, Inject, Input, Output, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { Backup } from '../../backup';
import { EditBackupService } from '../../services/edit-backup.service';
import { PassphraseService } from '../../services/passphrase.service';
import { SystemInfo } from '../../system-info/system-info';
import { SystemInfoService } from '../../system-info/system-info.service';
import { BackupOptions } from '../backup-options';

@Component({
  selector: 'app-backup-general-settings',
  templateUrl: './backup-general-settings.component.html',
  styleUrls: ['./backup-general-settings.component.less']
})
export class BackupGeneralSettingsComponent {
  @Input({ required: true }) backup!: Backup;
  @Input({ required: true }) options!: BackupOptions;
  @Output() backupChange = new EventEmitter<Backup>();
  @Output() optionsChange = new EventEmitter<BackupOptions>();
  @Output() next = new EventEmitter<void>();

  showPassphrase: boolean = false;
  get name(): string {
    return this.backup.Name;
  }
  set name(v: string) {
    this.backup = { ...this.backup };
    this.backup.Name = v;
    this.backupChange.emit(this.backup);
  }
  get description():string {
    return this.backup.Description;
  }
  set description(v: string) {
    this.backup = { ...this.backup };
    this.backup.Description = v;
    this.backupChange.emit(this.backup);
  }
  get encryptionModule(): string {
    return this.options.encryptionModule;
  }
  set encryptionModule(v: string) {
    this.options = { ...this.options };
    this.options.encryptionModule = v;
    this.optionsChange.emit(this.options);
  }
  get passphrase(): string {
    return this.options.passphrase;
  }
  set passphrase(v: string) {
    this.options = { ...this.options };
    this.options.passphrase = v;
    this.optionsChange.emit(this.options);
    this.computePassPhraseStrength();
  }
  get repeatPassphrase(): string {
    return this.options.repeatPassphrase;
  }
  set repeatPassphrase(v: string) {
    this.options = { ...this.options };
    this.options.repeatPassphrase = v;
    this.optionsChange.emit(this.options);
    this.computePassPhraseStrength();
  }
  passphraseScore: string | number = '';
  passphraseScoreString?: string;


  systemInfo?: SystemInfo;

  private subscription?: Subscription;

  constructor(private systemInfoService: SystemInfoService,
    private passphraseService: PassphraseService) { }

  ngOnInit() {
    this.subscription = this.systemInfoService.getState().subscribe(s => this.systemInfo = s);
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  togglePassphraseVisibility(): void {
    this.showPassphrase = !this.showPassphrase;
  }

  checkGpgAsymmetric(): boolean {
    return this.passphraseService.checkGpgAsymmetric(this.options.encryptionModule, this.options.extendedOptions);
  }

  generatePassphrase(): void {
    this.options = { ...this.options };
    this.options.passphrase = this.options.repeatPassphrase = this.passphraseService.generatePassphrase();
    this.showPassphrase = true;
    this.options.hasGeneratedPassphrase = true;
    this.optionsChange.emit(this.options);
  }

  computePassPhraseStrength(): void {
    let strengthMap: Record<string | number, string> = {
      'x': 'Passphrases do not match',
      0: 'Useless',
      1: 'Very weak',
      2: 'Weak',
      3: 'Strong',
      4: 'Very strong'
    };
    const passphrase = this.passphrase;
    if (this.repeatPassphrase !== passphrase) {
      this.passphraseScore = 'x';
    } else if (passphrase == '') {
      this.passphraseScore = '';
    } else {
      this.passphraseScore = this.passphraseService.computeStrength(this.passphrase);
    }

    this.passphraseScoreString = strengthMap[this.passphraseScore];
  }

  nextStep(): void { this.next.emit(); }
}