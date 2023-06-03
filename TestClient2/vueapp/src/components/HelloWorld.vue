<template>
  <div class="post">
    <input type="button" v-on:click="fetchUrl()" value="Log in" />
  </div>
</template>

<script lang="js">
import { defineComponent } from 'vue';

export default defineComponent({
    methods: {
        fetchUrl() {
            fetch('/api/v1/auth/url')
                .then(r => r.json())
                .then(json => {
                    window.location.href = json.url
                });
        }
    },
});
</script>
