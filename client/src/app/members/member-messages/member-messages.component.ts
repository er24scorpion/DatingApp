import { BusyService } from './../../_services/busy.service';
import { NgForm } from '@angular/forms';
import { MessageService } from './../../_services/message.service';
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Message } from 'src/app/models/message';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css'],
})
export class MemberMessagesComponent implements OnInit {
  @ViewChild('messageForm') messageForm!: NgForm;
  @Input() messages: Message[] = [];
  @Input() username = '';
  messageContent = '';
  loading = false;

  constructor(public messageService: MessageService) {}

  ngOnInit(): void {}

  sendMessage() {
    this.loading = true;
    this.messageService
      .sendMessage(this.username, this.messageContent)
      .then(() => {
        this.messageForm.reset();
      })
      .finally(() => (this.loading = false));
  }
}
