<panel *if={:CanEnable}>
  <frame border={@mushymato.MachineControlPanel/sprites/cursors:insetBg} border-thickness="6">
    <image
      sprite={@mushymato.MachineControlPanel/sprites/cursors:telescope}
      tooltip={#overlay-toggle.description}
      layout="48px 48px" +hover:scale="1.14"
      left-click=|ShowOverlay()|/>
  </frame>
</panel>
