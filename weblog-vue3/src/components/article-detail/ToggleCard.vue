<template>
  <div class="rounded-xl border border-[#e5e7eb] bg-white p-3">
    <div class="mb-2 text-sm font-bold text-[#0f172a]">{{ title }}</div>
    <div class="flex items-center justify-between gap-2">
      <span class="text-xs text-[#64748b]">{{ desc }}</span>
      <span :class="['relative h-5 w-9 rounded-full transition', on ? 'bg-[#2563eb]' : 'bg-slate-300']">
        <span :class="['absolute top-0.5 h-4 w-4 rounded-full bg-white transition', on ? 'left-4' : 'left-0.5']"></span>
      </span>
    </div>
  </div>
</template>

<script setup>
defineProps({
  title: { type: String, default: '' },
  desc: { type: String, default: '' },
  on: { type: Boolean, default: false }
})
</script>
