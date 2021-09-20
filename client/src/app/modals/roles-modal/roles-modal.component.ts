import { User } from './../../models/user';
import { Component, EventEmitter, Input, OnInit } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-roles-modal',
  templateUrl: './roles-modal.component.html',
  styleUrls: ['./roles-modal.component.css'],
})
export class RolesModalComponent implements OnInit {
  @Input() updateSelectedRoles = new EventEmitter();
  user!: User;
  roles: RoleModal[] = [];

  constructor(public bsModalRef: BsModalRef) {}

  ngOnInit(): void {}

  updateRoles() {
    this.updateSelectedRoles.emit(this.roles);
    this.bsModalRef.hide();
  }
}

export interface RoleModal {
  name: string;
  checked: boolean;
}
