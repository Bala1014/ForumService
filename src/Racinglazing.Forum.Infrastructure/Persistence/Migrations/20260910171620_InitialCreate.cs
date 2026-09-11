using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Racinglazing.Forum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    forum_count = table.Column<int>(type: "integer", nullable: false),
                    thread_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_by_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    thread_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forums",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    thread_count = table.Column<long>(type: "bigint", nullable: false),
                    post_count = table.Column<long>(type: "bigint", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forums", x => x.id);
                    table.ForeignKey(
                        name: "fk_forums_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "threads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    forum_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    race_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(340)", maxLength: 340, nullable: false),
                    root_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    upvote_count = table.Column<int>(type: "integer", nullable: false),
                    downvote_count = table.Column<int>(type: "integer", nullable: false),
                    comment_count = table.Column<int>(type: "integer", nullable: false),
                    view_count = table.Column<long>(type: "bigint", nullable: false),
                    hot_score = table.Column<double>(type: "double precision", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lock_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_threads", x => x.id);
                    table.ForeignKey(
                        name: "fk_threads_forums_forum_id",
                        column: x => x.forum_id,
                        principalTable: "forums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    content_text = table.Column<string>(type: "character varying(40000)", maxLength: 40000, nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    is_root = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: false),
                    upvote_count = table.Column<int>(type: "integer", nullable: false),
                    downvote_count = table.Column<int>(type: "integer", nullable: false),
                    reply_count = table.Column<int>(type: "integer", nullable: false),
                    hot_score = table.Column<double>(type: "double precision", nullable: false),
                    controversy_score = table.Column<double>(type: "double precision", nullable: false),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_comments_parent_comment_id",
                        column: x => x.parent_comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comments_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thread_tags",
                columns: table => new
                {
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thread_tags", x => new { x.thread_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_thread_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_thread_tags_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active_display_order",
                table: "categories",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comments_replies_new",
                table: "comments",
                columns: new[] { "parent_comment_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_replies_top",
                table: "comments",
                columns: new[] { "parent_comment_id", "score", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_thread_new",
                table: "comments",
                columns: new[] { "thread_id", "parent_comment_id", "is_root", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_thread_top",
                table: "comments",
                columns: new[] { "thread_id", "parent_comment_id", "is_root", "score", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_forums_category_id_display_order",
                table: "forums",
                columns: new[] { "category_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_forums_category_id_slug",
                table: "forums",
                columns: new[] { "category_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_queue",
                table: "reports",
                columns: new[] { "status", "target_type", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                table: "tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_tags_slug",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tags_thread_count",
                table: "tags",
                column: "thread_count");

            migrationBuilder.CreateIndex(
                name: "ix_thread_tags_tag_id",
                table: "thread_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_commented",
                table: "threads",
                columns: new[] { "forum_id", "is_deleted", "is_pinned", "comment_count", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_hot",
                table: "threads",
                columns: new[] { "forum_id", "is_deleted", "is_pinned", "hot_score", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_id_slug",
                table: "threads",
                columns: new[] { "forum_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_new",
                table: "threads",
                columns: new[] { "forum_id", "is_deleted", "is_pinned", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_top",
                table: "threads",
                columns: new[] { "forum_id", "is_deleted", "is_pinned", "score", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_forum_updated",
                table: "threads",
                columns: new[] { "forum_id", "is_deleted", "is_pinned", "last_activity_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_race_hot",
                table: "threads",
                columns: new[] { "race_id", "is_deleted", "is_pinned", "hot_score", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_votes_target_type_target_id",
                table: "votes",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_votes_user_id_target_type_target_id",
                table: "votes",
                columns: new[] { "user_id", "target_type", "target_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "thread_tags");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "threads");

            migrationBuilder.DropTable(
                name: "forums");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
